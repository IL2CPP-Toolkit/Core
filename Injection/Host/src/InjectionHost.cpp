#include "pch.h"
#include <grpcpp/grpcpp.h>
#include <grpcpp/health_check_service_interface.h>
#include <string>
#include <chrono>
#include <thread>
#include <winapifamily.h>
#include "win/WindowHelpers.h"
#include "win/Snapshot.h"
#include "PublicApi.h"
#include "InjectionHost.h"
#include "debug.h"

using namespace grpc;
using namespace std::chrono_literals;

const std::chrono::milliseconds InjectionHost::s_hookTTL{std::chrono::milliseconds(100)};

static std::shared_ptr<InjectionHost>& GetInstancePtr() noexcept
{
	static std::shared_ptr<InjectionHost> s_instance{std::make_shared<InjectionHost>()};
	return s_instance;
}

static void FreeGlobalInstance() noexcept
{
	GetInstancePtr().reset();
}

InjectionHostHandle::InjectionHostHandle(const std::shared_ptr<InjectionHost>& ptr) noexcept : owned{ptr} {}

InjectionHostHandle::~InjectionHostHandle() noexcept
{
	owned.reset();
}

InjectionHostHandle::operator bool() const noexcept
{
	return !!owned;
}

InjectionHost* InjectionHostHandle::operator->() const noexcept
{
	return owned.get();
}

std::recursive_mutex& InjectionHost::GetLock() const noexcept
{
	return const_cast<std::recursive_mutex&>(m_lock);
}

/* static */ InjectionHostHandle InjectionHost::GetInstance() noexcept
{
	return InjectionHostHandle{GetInstancePtr()};
}


void InjectionHost::RegisterProcess(uint32_t pid) noexcept
{
	{
		const std::lock_guard<std::recursive_mutex> lock(GetLock());
		DebugLog("RegisterProcess {}\n", pid);
		m_hasSetActivePid = true;
		m_activePids.insert(pid);
	}
}

void InjectionHost::DeregisterProcess(uint32_t pid) noexcept
{
	{
		const std::lock_guard<std::recursive_mutex> lock(GetLock());
		DebugLog("DeregisterProcess {}\n", pid);
		m_activePids.erase(pid);
	}
}

void InjectionHost::Detach() noexcept
{
	InjectionHostHandle self{GetInstance()};
	if (self)
		self->Shutdown();
	FreeGlobalInstance();
}

std::set<uint32_t> InjectionHost::ActivePidsSnapshot() const noexcept
{
	{
		const std::lock_guard<std::recursive_mutex> lock(GetLock());
		return std::set<uint32_t>(m_activePids);
	}
}

void InjectionHost::KeepAlive() noexcept
{
	m_tpKeepAliveExpiry = std::chrono::system_clock::now() + s_hookTTL;
}

void InjectionHost::ProcessMessages() noexcept
{
	m_executionQueue.DoWork();
}

/* static */ void InjectionHost::ServerThread() noexcept
{
	// do not capture InjectionHostHandle, or we can't shut down
	GetInstancePtr()->m_spServer->Wait();
}

/* static */ void InjectionHost::WatcherThread() noexcept
{
	InjectionHostHandle self{GetInstance()};
	while (true)
	{
		std::set<uint32_t> activePids = self->ActivePidsSnapshot();
		if (self->m_hasSetActivePid && activePids.size() == 0)
		{
			DebugLog("No registered processes remain\n");
			break;
		}

		Snapshot snapshot{0};
		for (const auto pid : activePids)
		{
			if (!snapshot.FindProcess(pid))
				self->DeregisterProcess(pid);
		}
		std::this_thread::sleep_for(s_hookTTL);
	}
	FreeGlobalInstance();
}

InjectionHost::InjectionHost() noexcept
	: m_tpKeepAliveExpiry{std::chrono::system_clock::now() + s_hookTTL}
	, m_executionQueue{}
	, m_spInjectionService{std::make_unique<InjectionServiceImpl>()}
	, m_spIl2cppService{std::make_unique<Il2CppServiceImpl>(m_executionQueue)}
{
	DebugLog("InjectionHost starting\n");
	ServerBuilder builder;
	builder.SetMaxSendMessageSize(-1);
	builder.SetMaxReceiveMessageSize(-1);
	builder.AddListeningPort("0.0.0.0:0", InsecureServerCredentials(), &PublicState::value.port);
	builder.RegisterService(m_spInjectionService.get());
	builder.RegisterService(m_spIl2cppService.get());
	m_spServer = builder.BuildAndStart();
	m_thWatcher = std::thread{InjectionHost::WatcherThread};
	m_thServer = std::thread{InjectionHost::ServerThread};
	DebugLog("InjectionHost started on port {}!\n", PublicState::value.port);

	DWORD dwPid{GetCurrentProcessId()};
	m_hwndMain = GetMainWindowForProcessId(dwPid, L"UnityWndClass");
	if (m_hwndMain)
	{
		int len{GetWindowTextLengthA(m_hwndMain) + 1};
		std::vector<char> buf(len);
		GetWindowTextA(m_hwndMain, &buf[0], len);

		std::string title(&buf[0]);
		m_originalTitle = title;

		title.append(" (Hooked)");
		SetWindowTextA(m_hwndMain, title.c_str());
	}
}

InjectionHost::~InjectionHost() noexcept
{
	if (m_hwndMain)
		SetWindowTextA(m_hwndMain, m_originalTitle.c_str());

	Shutdown();
}

void InjectionHost::Shutdown() noexcept
{
	DebugLog("InjectionHost::Shutdown\n");
	{
		const std::lock_guard<std::recursive_mutex> lock(GetLock());
		m_activePids.clear();
	}

	if (m_spServer)
	{
		const std::chrono::system_clock::time_point deadline{std::chrono::system_clock::now() + std::chrono::milliseconds(100)};
		m_spServer->Shutdown(deadline);
		m_executionQueue.Shutdown();
		m_thServer.join();

		// if shutting down on watcher thread, detach (to avoid `abort`)
		if (std::this_thread::get_id() == m_thWatcher.get_id())
			m_thWatcher.detach();
		else
			m_thWatcher.join();

		m_spServer.reset();

		m_spIl2cppService.reset();
	}
}

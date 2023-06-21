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

using namespace grpc;
using namespace std::chrono_literals;

const std::chrono::milliseconds InjectionHost::s_hookTTL{std::chrono::milliseconds(100)};

static std::recursive_mutex& GetLock() noexcept
{
	static std::recursive_mutex s_lock;
	return s_lock;
}

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

/* static */ InjectionHostHandle InjectionHost::GetInstance() noexcept
{
	return InjectionHostHandle{GetInstancePtr()};
}


void InjectionHost::RegisterProcess(uint32_t pid) noexcept
{
	const std::lock_guard<std::recursive_mutex> lock(GetLock());
	m_hasSetActivePid = true;
	m_activePids.insert(pid);
}

void InjectionHost::DeregisterProcess(uint32_t pid) noexcept
{
	const std::lock_guard<std::recursive_mutex> lock(GetLock());
	m_activePids.erase(pid);
}

void InjectionHost::Detach() noexcept
{
	InjectionHostHandle self{GetInstance()};
	self->Shutdown();
	FreeGlobalInstance();
}

std::set<uint32_t> InjectionHost::ActivePidsSnapshot() const noexcept
{
	const std::lock_guard<std::recursive_mutex> lock(GetLock());
	return std::set<uint32_t>(m_activePids);
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
		Snapshot snapshot{0};
		for (const auto pid : activePids)
		{
			if (!snapshot.FindProcess(pid))
				self->DeregisterProcess(pid);
		}

		activePids = self->ActivePidsSnapshot();
		if (self->m_hasSetActivePid && activePids.size() == 0)
			break;
		std::this_thread::sleep_for(s_hookTTL);
	}
	self->Shutdown();
	FreeGlobalInstance();
}

InjectionHost::InjectionHost() noexcept
	: m_tpKeepAliveExpiry{std::chrono::system_clock::now() + s_hookTTL}
	, m_executionQueue{}
	, m_spInjectionService{std::make_unique<InjectionServiceImpl>()}
	, m_spIl2cppService{std::make_unique<Il2CppServiceImpl>(m_executionQueue)}
{
	ServerBuilder builder;
	builder.AddListeningPort("0.0.0.0:0", InsecureServerCredentials(), &PublicState::value.port);
	builder.RegisterService(m_spInjectionService.get());
	builder.RegisterService(m_spIl2cppService.get());
	m_spServer = builder.BuildAndStart();
	m_thWatcher = std::thread{InjectionHost::WatcherThread};
	m_thServer = std::thread{InjectionHost::ServerThread};
}

InjectionHost::~InjectionHost() noexcept
{
	Shutdown();
}

void InjectionHost::Shutdown() noexcept
{
	const std::lock_guard<std::recursive_mutex> lock(GetLock());
	m_activePids.clear();

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

#include "pch.h"
#include <grpcpp/grpcpp.h>
#include <grpcpp/health_check_service_interface.h>
#include <string>
#include <chrono>
#include <thread>
#include <winapifamily.h>
#include "win/WindowHelpers.h"
#include "win/Snapshot.h"
#include "win/GlobalState.h"
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

void InjectionHost::Detach() noexcept
{
	std::thread{InjectionHost::DetachThread}.detach();
}

void InjectionHost::DetachThread() noexcept
{
	DebugLog("Detaching host\n");
	InjectionHostHandle self{GetInstance()};
	if (self)
		self->Shutdown();
	FreeGlobalInstance();
}

void InjectionHost::KeepAlive() noexcept {}

void InjectionHost::ProcessMessages() noexcept
{
	m_executionQueue.DoWork();
}

/* static */ void InjectionHost::ServerThread() noexcept
{
	// do not capture InjectionHostHandle, or we can't shut down
	GetInstancePtr()->m_spServer->Wait();
}

InjectionHost::InjectionHost() noexcept
	: m_executionQueue{}
	, m_spInjectionService{std::make_unique<InjectionServiceImpl>()}
	, m_spIl2cppService{std::make_unique<Il2CppServiceImpl>(m_executionQueue)}
{
	PublicState state{};
	DebugLog("InjectionHost starting\n");
	ServerBuilder builder;
	builder.SetMaxSendMessageSize(-1);
	builder.SetMaxReceiveMessageSize(-1);
	builder.AddListeningPort("0.0.0.0:0", InsecureServerCredentials(), &state.port);
	builder.RegisterService(m_spInjectionService.get());
	builder.RegisterService(m_spIl2cppService.get());
	m_spServer = builder.BuildAndStart();
	m_thServer = std::thread{InjectionHost::ServerThread};
	DebugLog("InjectionHost started on port {}!\n", state.port);

	DWORD dwPid{GetCurrentProcessId()};
	GlobalState::Instance()->SetGameProcessState(dwPid, state);

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

	if (m_spServer)
	{
		DWORD dwPid{GetCurrentProcessId()};
		GlobalState::Instance()->ClearGameProcessState(dwPid);

		const std::chrono::system_clock::time_point deadline{std::chrono::system_clock::now() + std::chrono::milliseconds(100)};
		m_spServer->Shutdown(deadline);
		m_executionQueue.Shutdown();
		m_thServer.join();
		m_spServer.reset();
		m_spIl2cppService.reset();
	}
}

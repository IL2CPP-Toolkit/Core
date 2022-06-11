#include "pch.h"
#include <grpcpp/grpcpp.h>
#include <grpcpp/health_check_service_interface.h>
#include <string>
#include <chrono>
#include <thread>
#include <winapifamily.h>
#include "service/MessageService.h"
#include "win/WindowHelpers.h"
#include "PublicApi.h"
#include "InjectionHost.h"

using namespace grpc;
using namespace std::chrono_literals;

const std::chrono::milliseconds InjectionHost::s_hookTTL{ std::chrono::milliseconds(5000) };

static std::unique_ptr<InjectionHost>& GetInstancePtr() noexcept
{
	static std::unique_ptr<InjectionHost> s_instance{ std::make_unique<InjectionHost>() };
	return s_instance;
}

void InjectionHost::Teardown() noexcept
{
	if (m_spServer)
	{
		const std::chrono::system_clock::time_point deadline{
			std::chrono::system_clock::now() +
			std::chrono::milliseconds(100) };
		m_spServer->Shutdown(deadline);
		m_executionQueue.Shutdown();
		m_thServer.join();

		// if shutting down on watcher thread, detach (to avoid `abort`)
		if (std::this_thread::get_id() == m_thWatcher.get_id())
			m_thWatcher.detach();
		else
			m_thWatcher.join();

		m_spServer.reset();

		m_spMessageService.reset();
		m_spIl2cppService.reset();
	}
}

/* static */ InjectionHost& InjectionHost::GetInstance() noexcept
{
	return *GetInstancePtr();
}

/* static */ void InjectionHost::ServerThread() noexcept
{
	InjectionHost& self{ GetInstance() };
	self.m_spServer->Wait();
}

/* static */ void InjectionHost::WatcherThread() noexcept
{
	InjectionHost& self{ GetInstance() };
	DWORD dwCurrentProcessId{ GetCurrentProcessId() };
	HWND hwndMain{ GetMainWindowForProcessId(dwCurrentProcessId, L"UnityWndClass")};
	while (std::chrono::system_clock::now() < self.m_tpKeepAliveExpiry) // || IsDebuggerPresent())
	{
		SendMessage(hwndMain, WM_NULL, 0, 0);
		std::this_thread::sleep_for(s_hookTTL);
	}
	self.Teardown();
}

InjectionHost::InjectionHost() noexcept
	: m_tpKeepAliveExpiry{ std::chrono::system_clock::now() + s_hookTTL }
	, m_executionQueue{}
	, m_spMessageService{ std::make_unique<MessageServiceImpl>(m_executionQueue) }
	, m_spIl2cppService{ std::make_unique<Il2CppServiceImpl>(m_executionQueue) }
{
	ServerBuilder builder;
	builder.AddListeningPort("0.0.0.0:0", InsecureServerCredentials(), &PublicState::value.port);
	builder.RegisterService(m_spMessageService.get());
	builder.RegisterService(m_spIl2cppService.get());
	m_spServer = builder.BuildAndStart();
	m_thWatcher = std::thread{ InjectionHost::WatcherThread };
	m_thServer = std::thread{ InjectionHost::ServerThread };
}

InjectionHost::~InjectionHost() noexcept
{
	Teardown();
}

void InjectionHost::KeepAlive() noexcept
{
	m_tpKeepAliveExpiry = std::chrono::system_clock::now() + s_hookTTL;
}

void InjectionHost::ProcessMessages() noexcept
{
	m_executionQueue.DoWork();
}

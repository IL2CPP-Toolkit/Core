#include "pch.h"
#include <grpcpp/grpcpp.h>
#include <grpcpp/health_check_service_interface.h>
#include <string>
#include <chrono>
#include <thread>
#include <winapifamily.h>
#include "InjectionHost.h"
#include "PublicApi.h"
#include "MessageService.h"
#include "WindowHelpers.h"

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
	HWND hwndMain{ GetMainWindowForProcessId(dwCurrentProcessId) };
	while (std::chrono::system_clock::now() < self.m_tpKeepAliveExpiry)
	{
		SendMessage(hwndMain, WM_NULL, 0, 0);
		std::this_thread::sleep_for(s_hookTTL);
	}
	self.Teardown();
}

InjectionHost::InjectionHost() noexcept
	: m_tpKeepAliveExpiry{ std::chrono::system_clock::now() + s_hookTTL }
	, m_executionQueue{}
	, m_messageService{ m_executionQueue }
{
	ServerBuilder builder;
	builder.AddListeningPort("0.0.0.0:0", InsecureServerCredentials(), &PublicState::value.port);
	builder.RegisterService(&m_messageService);
	m_spServer = builder.BuildAndStart();
	m_thWatcher = std::thread{ InjectionHost::WatcherThread };
	m_thServer = std::thread{ InjectionHost::ServerThread };
}

InjectionHost::~InjectionHost() noexcept
{
	Teardown();
}

void InjectionHost::ProcessMessages() noexcept
{
	m_tpKeepAliveExpiry = std::chrono::system_clock::now() + s_hookTTL;
	m_executionQueue.DoWork();
}

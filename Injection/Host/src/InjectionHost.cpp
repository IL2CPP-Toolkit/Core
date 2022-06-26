#include "pch.h"
#include <grpcpp/grpcpp.h>
#include <grpcpp/health_check_service_interface.h>
#include <string>
#include <chrono>
#include <thread>
#include <winapifamily.h>
#include "win/WindowHelpers.h"
#include "PublicApi.h"
#include "InjectionHost.h"

using namespace grpc;
using namespace std::chrono_literals;

const std::chrono::milliseconds InjectionHost::s_hookTTL{std::chrono::milliseconds(5000)};

static std::shared_ptr<InjectionHost> &GetInstancePtr() noexcept
{
	static std::shared_ptr<InjectionHost> s_instance{std::make_shared<InjectionHost>()};
	return s_instance;
}

static void FreeGlobalInstance() noexcept
{
	GetInstancePtr().reset();
}

InjectionHostHandle::InjectionHostHandle(const std::shared_ptr<InjectionHost> &ptr) noexcept
	: owned{ptr}
{
}

InjectionHostHandle::~InjectionHostHandle() noexcept
{
	owned.reset();
}

InjectionHostHandle::operator bool() const noexcept
{
	return !!owned;
}

InjectionHost *InjectionHostHandle::operator->() const noexcept
{
	return owned.get();
}

/* static */ InjectionHostHandle InjectionHost::GetInstance() noexcept
{
	return InjectionHostHandle{GetInstancePtr()};
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
	DWORD dwCurrentProcessId{GetCurrentProcessId()};
	HWND hwndMain{GetMainWindowForProcessId(dwCurrentProcessId, L"UnityWndClass")};
	uint32_t nTicks{0};
	while (std::chrono::system_clock::now() < self->m_tpKeepAliveExpiry)
	{
		++nTicks;
		std::this_thread::sleep_for(s_hookTTL / 10);
		SendMessage(hwndMain, WM_NULL, 0, 0);
	}
	self->Shutdown();
	FreeGlobalInstance();
}

InjectionHost::InjectionHost() noexcept
	: m_tpKeepAliveExpiry{std::chrono::system_clock::now() + s_hookTTL}, m_executionQueue{}, m_spIl2cppService{std::make_unique<Il2CppServiceImpl>(m_executionQueue)}
{
	ServerBuilder builder;
	builder.AddListeningPort("0.0.0.0:0", InsecureServerCredentials(), &PublicState::value.port);
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
	if (m_spServer)
	{
		const std::chrono::system_clock::time_point deadline{
			std::chrono::system_clock::now() +
			std::chrono::milliseconds(100)};
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

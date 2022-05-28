#include "pch.h"
#include <grpcpp/grpcpp.h>
#include <grpcpp/health_check_service_interface.h>
#include <string>
#include <winapifamily.h>
#include "InjectionHost.h"

using namespace grpc;

/* static */ InjectionHost& InjectionHost::GetInstance() noexcept
{
	return *GetInstancePtr();
}

/* static */ std::unique_ptr<InjectionHost>& InjectionHost::GetInstancePtr() noexcept
{
	static std::unique_ptr<InjectionHost> s_instance{ std::make_unique<InjectionHost>() };
	return s_instance;
}

/* static */ void InjectionHost::Teardown() noexcept
{
	GetInstancePtr().release();
}

/* static */ void InjectionHost::ServerThread(int port) noexcept
{
	ServerBuilder builder;
	const std::string server_address{ "0.0.0.0:" + std::to_string(port) };
	builder.AddListeningPort(server_address, InsecureServerCredentials());
	GetInstance().m_spServer = builder.BuildAndStart();
	GetInstance().m_spServer->Wait();
}

InjectionHost::InjectionHost() noexcept
	: m_iPort{50051}
{
	m_thServer = std::thread{ InjectionHost::ServerThread, m_iPort };
}

InjectionHost::~InjectionHost() noexcept
{
	if (m_spServer)
	{
		std::chrono::system_clock::time_point deadline{
			std::chrono::system_clock::now() +
			std::chrono::milliseconds(100) };
		m_spServer->Shutdown(deadline);
	}
}

#pragma once
#include<memory>
#include<thread>

// fwd decls
namespace grpc {
	class Server;
}

class InjectionHost
{
private:
	static std::unique_ptr<InjectionHost>& GetInstancePtr() noexcept;
	static void Teardown() noexcept;
public:
	InjectionHost() noexcept;
	~InjectionHost() noexcept;
	static InjectionHost& GetInstance() noexcept;

	uint32_t Port() const noexcept { return m_iPort; }
private:
	static void ServerThread(int port) noexcept;
	std::unique_ptr<grpc::Server> m_spServer;
	std::thread m_thServer;
	uint32_t m_iPort;
};


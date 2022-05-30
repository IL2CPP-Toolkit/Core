#pragma once
#include<memory>
#include<thread>
#include<queue>

// fwd decls
namespace grpc {
	class Server;
	class ServerCompletionQueue;
}

class InjectionHost
{
public:
	static InjectionHost& GetInstance() noexcept;
	InjectionHost() noexcept;
	virtual ~InjectionHost() noexcept;

	void ProcessMessages() noexcept;
	uint32_t Port() const noexcept { return m_iPort; }
private:
	static void WatcherThread() noexcept;
	static const std::chrono::milliseconds s_hookTTL;
	
	std::unique_ptr<grpc::ServerCompletionQueue> m_spCompletionQueue;
	std::unique_ptr<grpc::Server> m_spServer;

	std::thread m_thWatcher;
	std::chrono::system_clock::time_point m_tpKeepAliveExpiry;
	uint32_t m_iPort;
};


#pragma once
#include <memory>
#include <thread>
#include <queue>
#include "MessageService.h"
#include "safe_queue.h"
#include "ExecutionQueue.h"

// fwd decls
namespace grpc
{
	class Server;
	class ServerCompletionQueue;
}
namespace rtk
{
	class MessageServiceImpl;
}
class InjectionHost
{
public:
	static InjectionHost &GetInstance() noexcept;
	InjectionHost() noexcept;
	virtual ~InjectionHost() noexcept;

	void ProcessMessages() noexcept;
	uint32_t Port() const noexcept { return m_iPort; }

private:
	void Teardown() noexcept;
	static void ServerThread() noexcept;
	static void WatcherThread() noexcept;
	static const std::chrono::milliseconds s_hookTTL;

	std::unique_ptr<grpc::Server> m_spServer;
	MessageServiceImpl m_messageService;

	std::thread m_thWatcher;
	std::thread m_thServer;
	std::chrono::system_clock::time_point m_tpKeepAliveExpiry;
	ExecutionQueue m_executionQueue;
	uint32_t m_iPort;
};

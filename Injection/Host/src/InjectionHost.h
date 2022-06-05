#pragma once
#include <memory>
#include <thread>
#include <queue>
#include "safe_queue.h"
#include "ExecutionQueue.h"
#include "service/MessageService.h"
#include "service/Il2CppService.h"

// fwd decls
namespace grpc
{
	class Server;
	class ServerCompletionQueue;
}
namespace messageService
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
	Il2CppServiceImpl m_il2cppService;

	std::thread m_thWatcher;
	std::thread m_thServer;
	std::chrono::system_clock::time_point m_tpKeepAliveExpiry;
	ExecutionQueue m_executionQueue;
	uint32_t m_iPort;
};

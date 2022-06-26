#pragma once
#include <memory>
#include <thread>
#include <queue>
#include "safe_queue.h"
#include "ExecutionQueue.h"
#include "service/Il2CppService.h"

// fwd decls
namespace grpc
{
	class Server;
	class ServerCompletionQueue;
}

class InjectionHostHandle;

class InjectionHost
{
public:
	static InjectionHostHandle GetInstance() noexcept;
	InjectionHost() noexcept;
	virtual ~InjectionHost() noexcept;

	void KeepAlive() noexcept;
	void ProcessMessages() noexcept;
	uint32_t Port() const noexcept { return m_iPort; }

	void Shutdown() noexcept;

private:
	static void ServerThread() noexcept;
	static void WatcherThread() noexcept;
	static const std::chrono::milliseconds s_hookTTL;

	std::unique_ptr<grpc::Server> m_spServer;
	std::unique_ptr<Il2CppServiceImpl> m_spIl2cppService;

	std::thread m_thWatcher;
	std::thread m_thServer;
	std::chrono::system_clock::time_point m_tpKeepAliveExpiry;
	ExecutionQueue m_executionQueue;
	uint32_t m_iPort;
};

class InjectionHostHandle
{
public:
	InjectionHostHandle(const std::shared_ptr<InjectionHost> &ptr) noexcept;
	~InjectionHostHandle() noexcept;

	InjectionHost *operator->() const noexcept;
	operator bool() const noexcept;

private:
	std::shared_ptr<InjectionHost> owned;
};

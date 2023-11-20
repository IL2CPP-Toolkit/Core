#pragma once
#include <memory>
#include <thread>
#include <queue>
#include <set>
#include "safe_queue.h"
#include "ExecutionQueue.h"
#include "service/Il2CppService.h"
#include "service/InjectionService.h"
#include "MessageHandler.h"

// fwd decls
namespace grpc {
class Server;
class ServerCompletionQueue;
} // namespace grpc

class InjectionHostHandle;

class InjectionHost
{
public:
	static InjectionHostHandle GetInstance() noexcept;
	InjectionHost() noexcept;
	virtual ~InjectionHost() noexcept;

	void KeepAlive() noexcept;
	void ProcessMessages() noexcept;

	void Shutdown() noexcept;

	void Detach() noexcept;

private:
	static void DetachThread() noexcept;
	static void ServerThread() noexcept;
	static const std::chrono::milliseconds s_hookTTL;
	std::recursive_mutex& GetLock() const noexcept;

	std::unique_ptr<grpc::Server> m_spServer;
	std::unique_ptr<Il2CppServiceImpl> m_spIl2cppService;
	std::unique_ptr<InjectionServiceImpl> m_spInjectionService;

	MessageHandlerHook m_messageHandlerHook;
	std::thread m_thServer;
	std::string m_originalTitle;
	HWND m_hwndMain{};
	std::recursive_mutex m_lock;
	ExecutionQueue m_executionQueue;
};

class InjectionHostHandle
{
public:
	InjectionHostHandle(const std::shared_ptr<InjectionHost>& ptr) noexcept;
	~InjectionHostHandle() noexcept;

	InjectionHost* operator->() const noexcept;
	operator bool() const noexcept;

private:
	std::shared_ptr<InjectionHost> owned;
};

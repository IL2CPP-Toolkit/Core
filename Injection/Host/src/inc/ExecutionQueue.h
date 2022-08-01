#pragma once
#include <functional>
#include <optional>
#include <atomic>
#include <thread>
#include "safe_queue.h"

class ExecutionQueue
{
public:
	ExecutionQueue() noexcept {}
	void Shutdown() noexcept
	{
		q.clear();
		isShutdown.store(true);
	}

	template<typename T>
	std::optional<T> Invoke(std::function<T()>&& task) noexcept
	{
		std::optional<T> result{std::nullopt};
		std::atomic<bool> finished{false};

		if (isShutdown.load())
			return result;

		q.enqueue([task = std::move(task), &result, &finished]() mutable noexcept {
			try
			{
				result = task();
			}
			catch (...)
			{
				// don't crash the process
			}
			finished.store(true);
		});

		while (!finished.load() && !isShutdown.load())
			std::this_thread::yield();
		return result;
	}

	void DoWork() noexcept
	{
		if (isShutdown.load())
			return;

		std::function<void()> fn;
		while (q.try_dequeue(fn))
			fn();
	}

private:
	std::atomic<bool> isShutdown{false};
	safe_queue<std::function<void()>> q;
};

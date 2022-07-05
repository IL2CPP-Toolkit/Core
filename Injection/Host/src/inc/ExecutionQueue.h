#pragma once
#include <functional>
#include <optional>
#include "safe_queue.h"

class ExecutionQueue
{
public:
	ExecutionQueue() noexcept {}
	void Shutdown() noexcept
	{
		q.clear();
		isShutdown = true;
	}

	template<typename T>
	std::optional<T> Invoke(std::function<T()>&& task) noexcept
	{
		bool finished{ false };
		std::optional<T> result{ std::nullopt };

		if (isShutdown)
			return result;

		q.enqueue([task = std::move(task), &result, &finished]() mutable noexcept
		{
			result = task();
			finished = true;
		});
		
		while (!finished && !isShutdown);

		return result;
	}

	void DoWork() noexcept
	{
		if (isShutdown)
			return;

		std::function<void()> fn;
		while(q.try_dequeue(fn)) fn();
	}
private:
	bool isShutdown{ false };
	safe_queue<std::function<void()>> q;
};
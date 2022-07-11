#pragma once
#include <queue>
#include <mutex>
#include <condition_variable>

// A threadsafe-queue.
template <class T>
class safe_queue
{
public:
    safe_queue(void)
        : q{}
        , m{}
    {}

    ~safe_queue(void)
    {}

    void clear()
    {
        std::unique_lock<std::mutex> lock{ m };
        std::queue<T> u;
        std::swap(q, u);
    }

    // Add an element to the queue.
    void enqueue(T t)
    {
        std::unique_lock<std::mutex> lock{ m };
        q.push(t);
    }

    bool try_dequeue(T& value)
    {
        std::unique_lock<std::mutex> lock{ m };
        if (q.empty())
            return false;
        value = std::move(q.front());
        q.pop();
        return true;
    }

private:
    std::queue<T> q;
    mutable std::mutex m;
};
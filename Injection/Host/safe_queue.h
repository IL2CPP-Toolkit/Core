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
        , c{}
    {}

    ~safe_queue(void)
    {}

    void clear()
    {
        std::lock_guard<std::mutex> lock{ m };
        std::queue<T> u;
        std::swap(q, u);
        c.notify_one();
    }

    // Add an element to the queue.
    void enqueue(T t)
    {
        std::lock_guard<std::mutex> lock{ m };
        q.push(t);
        c.notify_one();
    }

    // Get the "front"-element.
    // If the queue is empty, wait till a element is avaiable.
    T dequeue(void)
    {
        std::unique_lock<std::mutex> lock{ m };
        while (q.empty())
        {
            // release lock as long as the wait and reaquire it afterwards.
            c.wait(lock);
        }
        T val{ q.front() };
        q.pop();
        return val;
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
    std::condition_variable c;
};
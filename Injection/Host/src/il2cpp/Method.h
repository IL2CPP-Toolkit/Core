#pragma once
#include <string>

struct Il2CppClass;

template<typename TReturn, typename... TArgs>
struct Method
{
private:
    typedef TReturn(*MethodRef)(TArgs...);
    MethodRef method;
public:
    Method(void* pMethod)
        : method{ reinterpret_cast<MethodRef>(pMethod) }
    {
    }
    Method(HMODULE hModule, const std::string& szMethodName)
        : method{ reinterpret_cast<MethodRef>(GetProcAddress(hModule, szMethodName.c_str())) }
    {
    }
    TReturn operator() (TArgs... args)
    {
        return method(args...);
    }
};

#pragma once
#include <string>
#include <wtypes.h>

struct imported_module
{
private:
	const char* szModuleName;

public:
	imported_module(const char* szName) noexcept
		: szModuleName{ szName }
	{}

	operator const HMODULE& () const noexcept
	{
		static HMODULE hMod{ GetModuleHandleA(szModuleName) };
		return hMod;
	}
};

template<typename S>
struct imported_method;

template<typename R, typename... Args>
struct imported_method<R(Args...)>
{
private:
	typedef R(*_Fty)(Args...);
	const imported_module& hModule;
	const char* szMethod;

public:
	imported_method(const imported_module& hFrom, const char* szName)
		: szMethod{ szName }
		, hModule{ hFrom }
	{ }
	R operator() (Args... args)
	{
		static _Fty method{ reinterpret_cast<_Fty>(GetProcAddress(hModule, szMethod)) };
		return method(args...);
	}
};

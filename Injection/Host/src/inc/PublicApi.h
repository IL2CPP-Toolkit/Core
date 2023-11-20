#pragma once
#include "framework.h"

enum class InjectResult : DWORD
{
	Unhooked = 0,
	Hooked = 1,
};

struct PublicState
{
	int port;
	PublicState() : port{-1} {}
	PublicState(const PublicState& other) : port{other.port} {}
	PublicState& operator=(const PublicState& other) noexcept
	{
		port = other.port;
		return *this;
	}
};

extern "C" __declspec(dllexport) void CALLBACK HookProcess(HWND hWnd, HINSTANCE hInst, LPWSTR lpszCmdLine, int nCmdShow);
extern "C" __declspec(dllexport) void CALLBACK UnhookAll(HWND hWnd, HINSTANCE hInst, LPWSTR lpszCmdLine, int nCmdShow);

extern "C" __declspec(dllexport) HRESULT WINAPI InjectHook(DWORD procId) noexcept;
extern "C" __declspec(dllexport) HRESULT WINAPI ReleaseHook(DWORD procId) noexcept;
/* @deprecated */
extern "C" __declspec(dllexport) InjectResult WINAPI GetHookState(DWORD procId) noexcept;
extern "C" __declspec(dllexport) HRESULT WINAPI GetState(DWORD procId, PublicState* pState, long timeoutMs = 3000) noexcept;

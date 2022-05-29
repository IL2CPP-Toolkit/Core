#pragma once
#include "framework.h"

enum class InjectResult : DWORD
{
	Unhooked = 0,
	Hooked = 1,
};

struct PublicState
{
	static PublicState value;
	int port{ -1 };
};

extern "C" __declspec(dllexport) HRESULT WINAPI InjectHook(DWORD procId);
extern "C" __declspec(dllexport) HRESULT WINAPI ReleaseHook(DWORD procId);
/* @deprecated */
extern "C" __declspec(dllexport) InjectResult WINAPI GetHookState(DWORD procId);
extern "C" __declspec(dllexport) HRESULT WINAPI GetState(DWORD procId, PublicState* pState, long timeoutMs = 1000);

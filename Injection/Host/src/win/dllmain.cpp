// dllmain.cpp : Defines the entry point for the DLL application.
#include "pch.h"
#include <winapifamily.h>
#include <MinHook.h>
#include "Snapshot.h"
#include "PublicApi.h"
#include "WindowHelpers.h"
#include "../MessageHandler.h"
#include "../InjectionHost.h"


template<typename... Args>
void DebugLog(std::string_view rt_fmt_str, Args&&... args)
{
	std::string line = std::vformat(rt_fmt_str, std::make_format_args(args...));
	OutputDebugStringA(line.c_str());
}

bool IsUnityProcess() noexcept
{
	HMODULE hUnityPlayer{GetModuleHandleA("UnityPlayer.dll")};
	return !!hUnityPlayer;
}

#define CHECKED_CALL(method, params)                                                                                                       \
	if ((mhStatus = method params) != MH_STATUS::MH_OK)                                                                                    \
	{                                                                                                                                      \
		OutputDebugStringA("ERROR: " #method " returned an error:");                                                                       \
		OutputDebugStringA(MH_StatusToString(mhStatus));                                                                                   \
		return TRUE;                                                                                                                       \
	}

BOOL APIENTRY DllMain(HMODULE hModule, DWORD ul_reason_for_call, LPVOID lpReserved)
{
	if (!IsUnityProcess())
		return TRUE;

	switch (ul_reason_for_call)
	{
		case DLL_PROCESS_ATTACH: {
			OutputDebugStringA("DLL_PROCESS_ATTACH\n");
			InjectionHost::GetInstance(); // force the host to initialize and hook PeekMessage
			return TRUE;
		}
		case DLL_PROCESS_DETACH: {
			OutputDebugStringA("DLL_PROCESS_DETACH\n");
			return TRUE;
		}
		case DLL_THREAD_ATTACH:
			return TRUE;
		case DLL_THREAD_DETACH:
			return TRUE;
		default:
			return TRUE;
	}
}

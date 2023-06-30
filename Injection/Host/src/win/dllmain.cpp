// dllmain.cpp : Defines the entry point for the DLL application.
#include "pch.h"
#include <winapifamily.h>
#include <MinHook.h>
#include "Snapshot.h"
#include "PublicApi.h"
#include "WindowHelpers.h"
#include "../MessageHandler.h"
#include "../InjectionHost.h"

bool IsUnityProcess() noexcept
{
	HMODULE hUnityPlayer{GetModuleHandleA("UnityPlayer.dll")};
	return !!hUnityPlayer;
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

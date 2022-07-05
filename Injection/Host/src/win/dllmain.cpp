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
	HMODULE hUnityPlayer{ GetModuleHandleA("UnityPlayer.dll") };
	return !!hUnityPlayer;
}

#define CHECKED_CALL(method, params) if ((mhStatus = method params ) != MH_STATUS::MH_OK) { \
	OutputDebugStringA("ERROR: " #method " returned an error:"); \
	OutputDebugStringA(MH_StatusToString(mhStatus)); \
	return TRUE; \
} \

BOOL APIENTRY DllMain(HMODULE hModule, DWORD ul_reason_for_call, LPVOID lpReserved)
{
	if (!IsUnityProcess())
		return TRUE;

	switch (ul_reason_for_call) {
	case DLL_PROCESS_ATTACH:
	{
		MH_STATUS mhStatus{ MH_STATUS::MH_OK };
		CHECKED_CALL(MH_Initialize, ());
		CHECKED_CALL(MH_CreateHook, (&PeekMessageA, &PeekMessage_Injected, reinterpret_cast<LPVOID*>(&pfnPeekMessage)));
		CHECKED_CALL(MH_EnableHook, (&PeekMessageA));
		return TRUE;
	}
	case DLL_PROCESS_DETACH:
	{
		MH_STATUS mhStatus{ MH_STATUS::MH_OK };
		CHECKED_CALL(MH_Uninitialize, ());
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

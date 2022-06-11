// dllmain.cpp : Defines the entry point for the DLL application.
#include "pch.h"
#include <winapifamily.h>
#include <MinHook.h>
#include "Snapshot.h"
#include "PublicApi.h"
#include "WindowHelpers.h"
#include "../MessageHandler.h"

BOOL APIENTRY DllMain(HMODULE hModule, DWORD ul_reason_for_call, LPVOID lpReserved)
{
	switch (ul_reason_for_call) {
	case DLL_PROCESS_ATTACH:
		{
			if (MH_Initialize() != MH_OK)
			{
				// TODO: Handle error
			}
			if (MH_CreateHook(&PeekMessageA, &PeekMessage_Injected, reinterpret_cast<LPVOID*>(&pfnPeekMessage)) != MH_OK)
			{
				// TODO: Handle error
			}
			MH_EnableHook(PeekMessageA);
			return TRUE;
		}
	case DLL_PROCESS_DETACH:
		if (MH_Uninitialize() != MH_OK)
		{
			// TODO: Handle error
		}
		return TRUE;
	case DLL_THREAD_ATTACH:
		return TRUE;
	case DLL_THREAD_DETACH:
		return TRUE;
	default:
		return TRUE;
	}
}

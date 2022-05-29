// dllmain.cpp : Defines the entry point for the DLL application.
#include "pch.h"
#include <string>
#include <winapifamily.h>
#include "Snapshot.h"
#include "MessageService.h"
#include "PublicApi.h"


#ifdef _DEBUG
extern "C"
__declspec(dllexport)
void CALLBACK Test(HWND hwnd, HINSTANCE hinst, LPSTR lpszCmdLine, int nCmdShow)
{
	Snapshot ss;
	if (!ss.FindProcess(L"notepad.exe"))
		return;
	DWORD dwProcId{ ss.Process().th32ProcessID };
	InjectHook(dwProcId);
	PublicState s;
	GetState(dwProcId, &s);
	s.port;
}
#endif

BOOL APIENTRY DllMain(HMODULE hModule, DWORD ul_reason_for_call, LPVOID lpReserved)
{
	switch (ul_reason_for_call) {
	case DLL_PROCESS_ATTACH:
		return TRUE;
	case DLL_PROCESS_DETACH:
		return TRUE;
	case DLL_THREAD_ATTACH:
		return TRUE;
	case DLL_THREAD_DETACH:
		return TRUE;
	default:
		return TRUE;
	}
}

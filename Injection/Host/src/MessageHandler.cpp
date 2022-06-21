#include "pch.h"
#include <vector>
#include "MessageHandler.h"
#include "InjectionHost.h"

FPeekMessage pfnPeekMessage{ nullptr };

BOOL WINAPI PeekMessage_Injected(
	_Out_ LPMSG lpMsg,
	_In_opt_ HWND hWnd,
	_In_ UINT wMsgFilterMin,
	_In_ UINT wMsgFilterMax,
	_In_ UINT wRemoveMsg)
{
	// must be first statement in method to avoid cleanup happening during execution
	{
		InjectionHostHandle spHost{ InjectionHost::GetInstance() };

		if (spHost)
		{
			spHost->ProcessMessages();
		}
		return pfnPeekMessage(lpMsg, hWnd, wMsgFilterMin, wMsgFilterMax, wRemoveMsg);
	}
}

extern "C"
__declspec(dllexport) LRESULT HandleHookedMessage(int code, WPARAM wParam, LPARAM lParam)
{
	// must be first statement in method to avoid cleanup happening during execution
	{
		InjectionHostHandle spHost{ InjectionHost::GetInstance() };

		const CWPSTRUCT* pMsg{ reinterpret_cast<CWPSTRUCT*>(lParam) };
		if (spHost)
		{
			spHost->KeepAlive();
		}
		return CallNextHookEx(NULL, code, wParam, lParam);
	}
}

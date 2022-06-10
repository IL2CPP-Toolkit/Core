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
	InjectionHost::GetInstance().ProcessMessages();
	return pfnPeekMessage(lpMsg, hWnd, wMsgFilterMin, wMsgFilterMax, wRemoveMsg);
}

extern "C"
__declspec(dllexport) LRESULT HandleHookedMessage(int code, WPARAM wParam, LPARAM lParam)
{
	const CWPSTRUCT* pMsg{ reinterpret_cast<CWPSTRUCT*>(lParam) };
	InjectionHost::GetInstance().ProcessMessages();
	return CallNextHookEx(NULL, code, wParam, lParam);
}

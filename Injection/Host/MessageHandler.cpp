#include "pch.h"
#include <vector>
#include "MessageHandler.h"
#include "InjectionHost.h"

extern "C"
__declspec(dllexport) LRESULT HandleHookedMessage(int code, WPARAM wParam, LPARAM lParam)
{
	const CWPSTRUCT* pMsg{ reinterpret_cast<CWPSTRUCT*>(lParam) };
	InjectionHost::GetInstance().ProcessMessages();
	return CallNextHookEx(NULL, code, wParam, lParam);
}

#include "pch.h"
#include <MinHook.h>
#include <vector>
#include "debug.h"
#include "MessageHandler.h"
#include "InjectionHost.h"


#define CHECKED_CALL(method, params)                                                                                                       \
	if ((mhStatus = method params) != MH_STATUS::MH_OK)                                                                                    \
	{                                                                                                                                      \
		OutputDebugStringA("ERROR: " #method " returned an error:");                                                                       \
		OutputDebugStringA(MH_StatusToString(mhStatus));                                                                                   \
		return TRUE;                                                                                                                       \
	}

FPeekMessage pfnPeekMessage{nullptr};

BOOL WINAPI
PeekMessage_Injected(_Out_ LPMSG lpMsg, _In_opt_ HWND hWnd, _In_ UINT wMsgFilterMin, _In_ UINT wMsgFilterMax, _In_ UINT wRemoveMsg)
{
	// must be first statement in method to avoid cleanup happening during execution
	{
		InjectionHostHandle spHost{InjectionHost::GetInstance()};

		if (spHost)
		{
			spHost->ProcessMessages();
		}
		return pfnPeekMessage(lpMsg, hWnd, wMsgFilterMin, wMsgFilterMax, wRemoveMsg);
	}
}

extern "C" __declspec(dllexport) LRESULT HandleHookedMessage(int code, WPARAM wParam, LPARAM lParam)
{
	{
		return CallNextHookEx(NULL, code, wParam, lParam);
	}
}

MessageHandlerHook::MessageHandlerHook() noexcept
{
	Attach();
}

MessageHandlerHook::~MessageHandlerHook() noexcept
{
	Detach();
}

BOOL MessageHandlerHook::Attach() noexcept
{
	MH_STATUS mhStatus{MH_STATUS::MH_OK};
	CHECKED_CALL(MH_Initialize, ());
	CHECKED_CALL(MH_CreateHook, (&PeekMessageA, &PeekMessage_Injected, reinterpret_cast<LPVOID*>(&pfnPeekMessage)));
	CHECKED_CALL(MH_EnableHook, (&PeekMessageA));
	m_isAttached = true;
	DebugLog(
		"Hooked PeekMessageA\n\t{} PeekMessageA\n\t{} PeekMessage_Injected\n",
		static_cast<void*>(&PeekMessageA),
		static_cast<void*>(&PeekMessage_Injected));
	return TRUE;
}

BOOL MessageHandlerHook::Detach() noexcept
{
	if (m_isAttached)
	{
		MH_STATUS mhStatus{MH_STATUS::MH_OK};
		CHECKED_CALL(MH_Uninitialize, ());
		OutputDebugStringA({"Unhooked PeekMessageA\n"});
	}
	return TRUE;
}
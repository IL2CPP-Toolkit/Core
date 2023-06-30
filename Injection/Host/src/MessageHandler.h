#pragma once

typedef BOOL(WINAPI* FPeekMessage)(LPMSG, HWND, UINT, UINT, UINT);

extern FPeekMessage pfnPeekMessage;

BOOL WINAPI
PeekMessage_Injected(_Out_ LPMSG lpMsg, _In_opt_ HWND hWnd, _In_ UINT wMsgFilterMin, _In_ UINT wMsgFilterMax, _In_ UINT wRemoveMsg);

extern "C" __declspec(dllexport) LRESULT HandleHookedMessage(int code, WPARAM wParam, LPARAM lParam);

class MessageHandlerHook
{
public:
	MessageHandlerHook() noexcept;
	~MessageHandlerHook() noexcept;

	BOOL Attach() noexcept;
	BOOL Detach() noexcept;

private:
	bool m_isAttached{false};
};

#pragma once
#include <wtypes.h>

struct FindWindowData
{
	DWORD procId;
	HWND hwnd;
	LPCWSTR lpwzWndClass;
	size_t ccWndClass;
};

BOOL CALLBACK EnumWindowCallback(HWND handle, LPARAM lParam) noexcept;
HWND GetMainWindowForProcessId(DWORD dwProcId, LPCWSTR lpwzWndClass) noexcept;
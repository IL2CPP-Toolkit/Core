#include "pch.h"
#include "WindowHelpers.h"

constexpr size_t MaxClassNameChars{ 250 };

BOOL CALLBACK EnumWindowCallback(HWND handle, LPARAM lParam) noexcept
{
	FindWindowData& data{ *reinterpret_cast<FindWindowData*>(lParam) };
	DWORD dwProcId;
	GetWindowThreadProcessId(handle, &dwProcId);
	if (data.procId != dwProcId || (GetWindow(handle, GW_OWNER) != (HWND)0 || !IsWindowVisible(handle)))
		return TRUE;

	wchar_t wzWndClass[MaxClassNameChars];
	if (GetClassName(handle, &wzWndClass[0], MaxClassNameChars) != data.ccWndClass || lstrcmpW(wzWndClass, data.lpwzWndClass) != 0)
		return TRUE;

	data.hwnd = handle;
	return FALSE;
}

HWND GetMainWindowForProcessId(DWORD dwProcId, LPCWSTR lpwzWndClass) noexcept
{
	FindWindowData findHwnd{ dwProcId, 0, lpwzWndClass, lstrlenW(lpwzWndClass) };
	if (EnumWindows(EnumWindowCallback, reinterpret_cast<LPARAM>(&findHwnd)) != FALSE)
	{
		return 0;
	}
	return findHwnd.hwnd;
}
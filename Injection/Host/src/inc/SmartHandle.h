#pragma once
#include <wtypes.h>

class SmartHandle
{
public:
	SmartHandle(HANDLE handle) noexcept : m_handle{ handle } {}
	~SmartHandle() noexcept { CloseHandle(m_handle); }
	operator HANDLE& () noexcept { return m_handle; }
private:
	HANDLE m_handle;
};
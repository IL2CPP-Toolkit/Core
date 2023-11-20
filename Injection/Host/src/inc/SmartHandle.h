#pragma once
#include <wtypes.h>

class SmartHandle
{
public:
	SmartHandle(HANDLE handle) noexcept : m_handle{ handle } {}
	~SmartHandle() noexcept { CloseHandle(m_handle); }
	operator HANDLE& () const noexcept { return const_cast<HANDLE&>(m_handle); }
private:
	HANDLE m_handle;
};
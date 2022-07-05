#include "pch.h"
#include <locale>
#include <codecvt>
#include <il2cpp/il2cpp-api.h>
#include "SystemString.h"
#include "Il2CppClassInfo.h"
#include "Il2CppContext.h"

const Il2CppClass* get_System_String() noexcept
{
	static const Il2CppClassInfo* s_pSystemString{ Il2CppContext::instance().FindClass("System.String") };
	return s_pSystemString->klass();
}

SystemString::SystemString(const std::string& value) noexcept
{
	static std::wstring_convert<std::codecvt_utf8_utf16<wchar_t>> converter;
	Init(converter.from_bytes(value));
}

SystemString::SystemString(const std::wstring& value) noexcept
{
	Init(value);
}

void SystemString::Init(const std::wstring& value) noexcept
{
	size_t cb{ sizeof(System_String_o) + value.length() * 2 + 2 };
	void* strAlloc{ il2cpp_alloc(cb) };
	if (!strAlloc)
		return;
	memset(strAlloc, 0, cb);
	m_pStr = reinterpret_cast<System_String_o*>(strAlloc);
	m_pStr->_object.klass = const_cast<Il2CppClass*>(get_System_String());
	m_pStr->_object.monitor = nullptr;
	m_pStr->fields.m_stringLength = static_cast<int32_t>(value.length());
	memcpy(&m_pStr->fields.m_firstChar, value.c_str(), value.length() * 2);
}


SystemString::~SystemString() noexcept
{
	il2cpp_free(m_pStr);
}

System_String_o* SystemString::Value() noexcept
{
	return m_pStr;
}
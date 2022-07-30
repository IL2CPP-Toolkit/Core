#include "pch.h"
#include "Il2CppHelpers.h"
#include <buffer.h>
#include <il2cpp/il2cpp-api.h>
#include <string>

static constexpr int MessageSizeLimit{4096};

bool il2cpp_chk_class(const Il2CppClass* pClass) noexcept
{
	return pClass && pClass->klass == pClass;
}

bool il2cpp_chk_object(const Il2CppObject* pObj) noexcept
{
	return pObj && il2cpp_chk_class(pObj->klass);
}

Il2CppObject* il2cpp_object_from_ptr(const void* pvObj) noexcept
{
	Il2CppObject* pObj{const_cast<Il2CppObject*>(reinterpret_cast<const Il2CppObject*>(pvObj))};
	if (!il2cpp_chk_object(pObj))
		return nullptr;
	return pObj;
}

Il2CppClass* il2cpp_klass_from_ptr(const void* pvClass) noexcept
{
	Il2CppClass* pClass{const_cast<Il2CppClass*>(reinterpret_cast<const Il2CppClass*>(pvClass))};
	if (!il2cpp_chk_class(pClass))
		return nullptr;
	return pClass;
}

std::string il2cpp_format_exception_to_string(const Il2CppException* ex) noexcept
{
	buffer szBuf{MessageSizeLimit};
	szBuf.fill();
	il2cpp_format_exception(ex, szBuf.data(), MessageSizeLimit);
	return szBuf.data();
}

std::string il2cpp_format_stack_trace_to_string(const Il2CppException* ex) noexcept
{
	buffer szBuf{MessageSizeLimit};
	szBuf.fill();
	il2cpp_format_stack_trace(ex, szBuf.data(), MessageSizeLimit);
	return szBuf.data();
}
#pragma once
#include <il2cpp/il2cpp-api.h>

Il2CppObject* il2cpp_object_from_ptr(const void* pvObj) noexcept;
Il2CppClass* il2cpp_klass_from_ptr(const void* pvClass) noexcept;
bool il2cpp_chk_class(const Il2CppClass* pClass) noexcept;
bool il2cpp_chk_object(const Il2CppObject* pObj) noexcept;
std::string il2cpp_format_exception_to_string(const Il2CppException* ex) noexcept;
std::string il2cpp_format_stack_trace_to_string(const Il2CppException* ex) noexcept;
#pragma once
#include <string>
#include <il2cpp/il2cpp-object-internals.h>

struct __declspec(align(8)) System_String_Fields {
	int32_t m_stringLength;
	uint16_t m_firstChar;
};

struct System_String_o {
    Il2CppObject _object{};
    System_String_Fields fields;
};

struct SystemString final
{
	void Init(const std::wstring& value) noexcept;
public:
	SystemString(const std::string& value) noexcept;
	SystemString(const std::wstring& value) noexcept;
	~SystemString() noexcept;
	System_String_o* Value() noexcept;
private:
	System_String_o* m_pStr{};
};
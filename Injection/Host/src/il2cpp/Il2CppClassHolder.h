#pragma once
#include <string>

struct MethodInfo;
struct Il2CppClass;

class Il2CppClassHolder
{
public:
	Il2CppClassHolder(Il2CppClass* pClass) noexcept;

	MethodInfo* FindMethod(const std::string& name, const int num_args) noexcept;
	Il2CppClass& Class() noexcept;
	bool Empty() const noexcept;
private:
	Il2CppClass* m_pClass{};
};


#pragma once

struct Il2CppClass;

class Il2CppClassInfo final
{
public:
	Il2CppClassInfo(const Il2CppClass* pClass) noexcept;
	const std::string& name() const noexcept;
	Il2CppClass* klass() const noexcept;

private:
	const std::string m_szName;
	const Il2CppClass* m_pClass;
};

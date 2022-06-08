#pragma once
#include <string>
#include <memory>
#include <map>

class Il2CppClassInfo;

class Il2CppContext
{
	static std::unique_ptr<Il2CppContext>& instance_ptr() noexcept;
public:
	Il2CppContext() noexcept;
	static Il2CppContext& instance() noexcept;
	static void teardown() noexcept;

	const Il2CppClassInfo* FindClass(const std::string& name) const noexcept;

private:
	void BuildTypeCache() noexcept;
	void CacheClass(const Il2CppClass* pClass) noexcept;
	std::map<std::string, const Il2CppClassInfo> m_typeCache;
};
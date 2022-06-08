#include "pch.h"
#include <il2cpp/il2cpp-api.h>
#include "Il2CppClassInfo.h"
#include "Il2CppContext.h"

/*static*/ std::unique_ptr<Il2CppContext>& Il2CppContext::instance_ptr() noexcept
{
	static std::unique_ptr<Il2CppContext> s_instance{ std::make_unique<Il2CppContext>() };
	return s_instance;
}

/*static*/ Il2CppContext& Il2CppContext::instance() noexcept
{
	return *instance_ptr();
}

/*static*/ void Il2CppContext::teardown() noexcept
{
	instance_ptr().reset();
}

Il2CppContext::Il2CppContext() noexcept
{
	BuildTypeCache();
}

const Il2CppClassInfo* Il2CppContext::FindClass(const std::string& name) const noexcept
{
	const auto findIter = m_typeCache.find(name);
	if (findIter != m_typeCache.cend())
	{
		return &findIter->second;
	}
	return nullptr;
}

void Il2CppContext::BuildTypeCache() noexcept
{
	const Il2CppDomain* pAppDomain{ il2cpp_domain_get() };
	size_t casm{};
	const Il2CppAssembly** ppAssemblies{ il2cpp_domain_get_assemblies(pAppDomain, &casm) };

	for (size_t n{ 0 }; n < casm; ++n)
	{
		const Il2CppAssembly* pAssembly{ *(ppAssemblies++) };
		const Il2CppImage* pImage{ il2cpp_assembly_get_image(pAssembly) };
		const size_t cclass{ il2cpp_image_get_class_count(pImage) };
		for (size_t iClass{ 0 }; iClass < cclass; ++iClass)
		{
			const Il2CppClass* pClass{ il2cpp_image_get_class(pImage, iClass) };
			CacheClass(pClass);
		}
	}
}

void Il2CppContext::CacheClass(const Il2CppClass* pClass) noexcept
{
	Il2CppClassInfo classInfo{ pClass };
	m_typeCache.insert({ classInfo.name(), std::move(classInfo) });
	if (pClass->parent)
		CacheClass(pClass->parent);
}

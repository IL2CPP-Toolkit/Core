#include "pch.h"
#include <il2cpp/il2cpp-api.h>
#include "Il2CppClassInfo.h"
#include "Il2CppContext.h"

/*static*/ std::unique_ptr<Il2CppContext>& Il2CppContext::instance_ptr() noexcept
{
	static std::unique_ptr<Il2CppContext> s_instance{std::make_unique<Il2CppContext>()};
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

const Il2CppClassInfo* Il2CppContext::FindClass(const std::string& namespaze, const std::string& name) const noexcept
{
	std::string fullName{namespaze};
	if (!fullName.empty())
	{
		fullName.append(".");
		fullName.append(name);
	}
	const Il2CppClassInfo* pClassInfo{FindClass(fullName)};
	if (pClassInfo)
	{
		return pClassInfo;
	}
	const Il2CppDomain* pAppDomain{il2cpp_domain_get()};
	size_t casm{};
	const Il2CppAssembly** ppAssemblies{il2cpp_domain_get_assemblies(pAppDomain, &casm)};

	for (size_t n{0}; n < casm; ++n)
	{
		const Il2CppAssembly* pAssembly{*(ppAssemblies++)};
		const Il2CppImage* pImage{il2cpp_assembly_get_image(pAssembly)};
		const Il2CppClass* pClass{il2cpp_class_from_name(pImage, namespaze.c_str(), name.c_str())};
		if (pClass)
		{
			CacheClass(pClass);
			pClassInfo = FindClass(fullName);
			return pClassInfo;
		}
	}
	return nullptr;
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

Il2CppObject* Il2CppContext::GetCppObject(const std::string& namespaze, const std::string& name, void* pInst) const noexcept
{
	const Il2CppClassInfo* pCls{FindClass(namespaze, name)};
	if (!pCls)
		return nullptr;

	Il2CppObject* pObj{static_cast<Il2CppObject*>(pInst)};
	if (!pObj)
		return nullptr;

	if (pCls->klass()->valuetype)
		return pObj;

	if (pObj->klass == pCls->klass() || il2cpp_class_is_subclass_of(pObj->klass, pCls->klass(), true))
		return pObj;

	return nullptr;
}

void Il2CppContext::BuildTypeCache() noexcept
{
	const Il2CppDomain* pAppDomain{il2cpp_domain_get()};
	size_t casm{};
	const Il2CppAssembly** ppAssemblies{il2cpp_domain_get_assemblies(pAppDomain, &casm)};

	for (size_t n{0}; n < casm; ++n)
	{
		const Il2CppAssembly* pAssembly{*(ppAssemblies++)};
		const Il2CppImage* pImage{il2cpp_assembly_get_image(pAssembly)};
		const size_t cclass{il2cpp_image_get_class_count(pImage)};
		for (size_t iClass{0}; iClass < cclass; ++iClass)
		{
			const Il2CppClass* pClass{il2cpp_image_get_class(pImage, iClass)};
			CacheClass(pClass);
		}
	}
}

void Il2CppContext::CacheClass(const Il2CppClass* pClass) const noexcept
{
	Il2CppClassInfo classInfo{pClass};
	m_typeCache.insert({classInfo.name(), std::move(classInfo)});
	if (pClass->parent)
		CacheClass(pClass->parent);
}

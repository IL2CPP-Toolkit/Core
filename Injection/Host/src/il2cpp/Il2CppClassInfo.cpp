#include "pch.h"
#include <il2cpp/il2cpp-api.h>
#include <string>
#include "Il2CppClassInfo.h"

static std::string BuildClassName(const Il2CppClass* pClass) noexcept
{
	std::string wzName{};
	if (pClass->namespaze && pClass->namespaze[0] != '\0')
	{
		wzName.append(pClass->namespaze);
		wzName.append(".");
	}
	wzName.append(pClass->name);
	if (pClass->generic_class)
	{
		// exclude `1
		wzName.resize(wzName.size() - 2);
		wzName.append("<");
		const Il2CppGenericInst& classInst{ *pClass->generic_class->context.class_inst };
		for (int n{ 0 }; n < classInst.type_argc; ++n)
		{
			if (n > 1) wzName.append(", ");

			if (classInst.type_argv[n]->type == Il2CppTypeEnum::IL2CPP_TYPE_CLASS)
			{
				const Il2CppClass* pArgClass{ il2cpp_class_from_type(classInst.type_argv[n]) };
				wzName.append(BuildClassName(pArgClass));
			}
		}
		wzName.append(">");
	}
	return wzName;
}

Il2CppClassInfo::Il2CppClassInfo(const Il2CppClass* pClass) noexcept
	: m_pClass{ pClass }
	, m_szName{ BuildClassName(pClass)}
{
	
}

const std::string& Il2CppClassInfo::name() const noexcept
{
	return m_szName;
}

Il2CppClass* Il2CppClassInfo::klass() const noexcept
{
	return const_cast<Il2CppClass*>(m_pClass);
}

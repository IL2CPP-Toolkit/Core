using System.Collections.Generic;

namespace Il2CppToolkit.Model
{
	public interface ITypeModel
	{
		Il2Cpp Il2Cpp { get; }
		Metadata Metadata { get; }
		string ModuleName { get; }
		IReadOnlyList<TypeDescriptor> TypeDescriptors { get; }
		IReadOnlyDictionary<Il2CppMethodDefinition, ulong> MethodAddresses { get; }
		IReadOnlyDictionary<Il2CppMethodSpec, ulong> MethodSpecAddresses { get; }
		IReadOnlyDictionary<Il2CppTypeDefinition, ulong> TypeDefToAddress { get; }
		IReadOnlyDictionary<int, TypeDescriptor> TypeDefsByIndex { get; }
	}

	public interface ITypeModelMetadata : ITypeModel
	{
		Il2CppGenericParameter GetGenericParameterFromIl2CppType(Il2CppType il2CppType);
		Il2CppTypeDefinition GetTypeDefinitionFromIl2CppType(Il2CppType il2CppType, bool resolveGeneric = true);
		long GetGenericClassTypeDefinitionIndex(Il2CppGenericClass genericClass);
		Il2CppTypeDefinition GetGenericClassTypeDefinition(Il2CppGenericClass genericClass);
	}
}
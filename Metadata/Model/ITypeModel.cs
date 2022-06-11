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
		IReadOnlyDictionary<Il2CppType, ulong> TypeToTypeInfoAddress { get; }
		IReadOnlyDictionary<int, TypeDescriptor> TypeDefsByIndex { get; }
		IReadOnlyDictionary<Il2CppTypeDefinition, Il2CppType[]> GenericClassList { get; }
	}

	public interface ITypeModelMetadata : ITypeModel
	{
		ulong GetFieldOffsetFromIndex(Il2CppTypeDefinition typeDefinition, int fieldIndex);
		Il2CppGenericParameter GetGenericParameterFromIl2CppType(Il2CppType il2CppType);
		Il2CppTypeDefinition GetTypeDefinitionFromIl2CppType(Il2CppType il2CppType, bool resolveGeneric = true);
		long GetGenericClassTypeDefinitionIndex(Il2CppGenericClass genericClass);
		Il2CppTypeDefinition GetGenericClassTypeDefinition(Il2CppGenericClass genericClass);
		bool TryGetDefaultValue(int typeIndex, int dataIndex, out object value);
		bool TryGetDefaultValueBytes(int typeIndex, int dataIndex, out byte[] value);
	}
}
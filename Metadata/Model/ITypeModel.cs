using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Il2CppToolkit.Model
{
	public interface ITypeModel
	{
		Il2Cpp Il2Cpp { get; }
		Metadata Metadata { get; }
		string ModuleName { get; }
		IReadOnlyList<TypeDescriptor> TypeDescriptors { get; }
	}

	public interface ITypeModelMetadata : ITypeModel
	{
		ulong GetFieldOffsetFromIndex(Il2CppTypeDefinition typeDefinition, int fieldIndex);
		Il2CppGenericParameter GetGenericParameterFromIl2CppType(Il2CppType il2CppType);
		Il2CppTypeDefinition GetTypeDefinitionFromIl2CppType(Il2CppType il2CppType, bool resolveGeneric = true);
		long GetGenericClassTypeDefinitionIndex(Il2CppGenericClass genericClass);
		Il2CppTypeDefinition GetGenericClassTypeDefinition(Il2CppGenericClass genericClass);
		bool TryGetTypeDescriptor(Il2CppTypeDefinition cppTypeDefinition, [NotNullWhen(true)] out TypeDescriptor? typeDescriptor);
		bool TryGetDefaultValue(Il2CppFieldDefaultValue defaultValue, out object value);
		bool TryGetDefaultValueBytes(Il2CppFieldDefaultValue defaultValue, out byte[] value);
	}
}
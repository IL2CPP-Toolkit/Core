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
}
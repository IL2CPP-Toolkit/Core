using Il2CppToolkit.Model;
using Mono.Cecil;

namespace Il2CppToolkit.ReverseCompiler.Target.NetCore
{
	public partial class ModuleBuilder
	{
		private void InitializeTypeDefinition(Il2CppTypeDefinition cppTypeDef, TypeDefinition typeDef)
		{
			// nested types
			for (int i = 0; i < cppTypeDef.nested_type_count; i++)
			{
				var nestedIndex = Metadata.nestedTypeIndices[cppTypeDef.nestedTypesStart + i];
				var nestedTypeDef = Metadata.typeDefs[nestedIndex];
				// any nested type must also be a TypeDefinition
				if (UseTypeDefinition(nestedTypeDef) is not TypeDefinition nestedTypeDefinition)
					continue;
				typeDef.NestedTypes.Add(nestedTypeDefinition);
			}

			// interface implementations
			for (int n = cppTypeDef.interfacesStart, m = cppTypeDef.interfacesStart + cppTypeDef.interfaces_count; n < m; ++n)
			{
				Il2CppType cppInterfaceType = Il2Cpp.Types[Metadata.interfaceIndices[n]];
				TypeReference interfaceRef = UseTypeReference(typeDef, cppInterfaceType);
				if (interfaceRef != null)
					typeDef.Interfaces.Add(new InterfaceImplementation(interfaceRef));
			}

			// genericParameters
			if (cppTypeDef.genericContainerIndex >= 0)
			{
				var genericContainer = Metadata.genericContainers[cppTypeDef.genericContainerIndex];
				for (int i = 0; i < genericContainer.type_argc; i++)
				{
					var genericParameterIndex = genericContainer.genericParameterStart + i;
					var param = Metadata.genericParameters[genericParameterIndex];
					var genericParameter = CreateGenericParameter(param, typeDef);
					typeDef.GenericParameters.Add(genericParameter);
				}
			}

			// parent
			if (cppTypeDef.parentIndex >= 0)
			{
				var parentType = Il2Cpp.Types[cppTypeDef.parentIndex];
				var parentTypeRef = UseTypeReference(typeDef, parentType);
				typeDef.BaseType = parentTypeRef;
			}
		}
	}
}
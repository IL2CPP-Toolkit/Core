using System.Linq;
using Il2CppToolkit.Model;
using Mono.Cecil;

namespace Il2CppToolkit.ReverseCompiler.Target.NetCore
{
	public partial class ModuleBuilder
	{
		private MethodDefinition FindMethod(TypeDefinition typeDef, int methodIndex)
		{
			if (!MethodDefs.TryGetValue(methodIndex, out MethodDefinition methodDef))
			{
				string methodName = Metadata.GetStringFromIndex(Metadata.methodDefs[methodIndex].nameIndex);
				methodDef = typeDef.Methods.FirstOrDefault(method => method.Name == methodName);
			}
			return methodDef;
		}
		private PropertyDefinition EnsurePropertyDefinition(TypeDefinition typeDef, string propertyName, TypeReference typeRef)
		{
			PropertyDefinition propertyDef = typeDef.Properties.FirstOrDefault(method => method.Name == propertyName);
			if (propertyDef != null)
				return propertyDef;

			propertyDef = new PropertyDefinition(propertyName, PropertyAttributes.None, typeRef);
			typeDef.Properties.Add(propertyDef);
			return propertyDef;
		}
		private void DefineProperties(Il2CppTypeDefinition cppTypeDef, TypeDefinition typeDef)
		{
			int propertyEnd = cppTypeDef.propertyStart + cppTypeDef.property_count;
			for (var i = cppTypeDef.propertyStart; i < propertyEnd; ++i)
			{
				Il2CppPropertyDefinition cppPropertyDef = Metadata.propertyDefs[i];
				string name = Metadata.GetStringFromIndex(cppPropertyDef.nameIndex);
				MethodDefinition getMethodDef = cppPropertyDef.get >= 0 ? FindMethod(typeDef, cppTypeDef.methodStart + cppPropertyDef.get) : null;
				MethodDefinition setMethodDef = cppPropertyDef.set >= 0 ? FindMethod(typeDef, cppTypeDef.methodStart + cppPropertyDef.set) : null;
				if (getMethodDef != null || setMethodDef != null)
				{
					PropertyDefinition propertyDef = EnsurePropertyDefinition(typeDef, name, getMethodDef?.ReturnType ?? setMethodDef.Parameters[0].ParameterType);
					propertyDef.GetMethod ??= getMethodDef;
					propertyDef.SetMethod ??= setMethodDef;
				}
			}
		}
	}
}
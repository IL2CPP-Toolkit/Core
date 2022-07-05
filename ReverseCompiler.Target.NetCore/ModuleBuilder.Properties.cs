using System;
using System.Linq;
using System.Text.RegularExpressions;
using Il2CppToolkit.Model;
using Il2CppToolkit.Runtime;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Il2CppToolkit.ReverseCompiler.Target.NetCore
{
	public partial class ModuleBuilder
	{
		private void DefineProperties(Il2CppTypeDefinition cppTypeDef, TypeDefinition typeDef)
		{
			int propertyEnd = cppTypeDef.propertyStart + cppTypeDef.property_count;
			for (var i = cppTypeDef.propertyStart; i < propertyEnd; ++i)
			{
				Il2CppPropertyDefinition cppPropertyDef = Metadata.propertyDefs[i];
				string name = Metadata.GetStringFromIndex(cppPropertyDef.nameIndex);
				bool hasSetter = MethodDefs.TryGetValue(cppTypeDef.methodStart + cppPropertyDef.set, out MethodDefinition setMethodDef);
				if (MethodDefs.TryGetValue(cppTypeDef.methodStart + cppPropertyDef.get, out MethodDefinition getMethodDef))
				{
					PropertyDefinition propertyDef = new PropertyDefinition(name, PropertyAttributes.None, getMethodDef.ReturnType)
					{
						GetMethod = getMethodDef,
						SetMethod = setMethodDef
					};
					typeDef.Properties.Add(propertyDef);
				}
				else if (hasSetter)
				{
					// we will have only setters registered as il2cpp-backed methods for fields that were promoted to properties
					// in this event, we should remove those setters from the type now
					typeDef.Methods.Remove(setMethodDef);
				}
			}
		}
	}
}
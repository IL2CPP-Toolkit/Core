using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Il2CppToolkit.Model;
using Il2CppToolkit.Runtime;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Il2CppToolkit.ReverseCompiler.Target.NetCore
{
	public partial class ModuleBuilder
	{
		private void DefineFields(Il2CppTypeDefinition cppTypeDef, TypeDefinition typeDef, TypeInfoBuilder typeInfo)
		{
			int fieldEnd = cppTypeDef.fieldStart + cppTypeDef.field_count;
			for (var i = cppTypeDef.fieldStart; i < fieldEnd; ++i)
			{
				Il2CppFieldDefinition cppFieldDef = Metadata.fieldDefs[i];
				Il2CppType cppFieldType = Il2Cpp.Types[cppFieldDef.typeIndex];
				string fieldName = Metadata.GetStringFromIndex(cppFieldDef.nameIndex);
				TypeReference fieldTypeRef = UseTypeReference(typeDef, cppFieldType);
				if (fieldTypeRef == null)
					continue;

				FieldAttributes fieldAttrs = (FieldAttributes)cppFieldType.attrs;
				bool isStatic = fieldAttrs.HasFlag(FieldAttributes.Static);

				if (fieldAttrs.HasFlag(FieldAttributes.Literal) || cppTypeDef.IsEnum)
				{
					FieldDefinition fld = new(fieldName, fieldAttrs, fieldTypeRef);
					if (fieldAttrs.HasFlag(FieldAttributes.Literal)
						&& Metadata.GetFieldDefaultValueFromIndex(i, out Il2CppFieldDefaultValue cppDefaultValue)
						&& cppDefaultValue.dataIndex != -1
						&& Context.Model.TryGetDefaultValueBytes(cppFieldDef.typeIndex, cppDefaultValue.dataIndex, out byte[] defaultValueBytes))
					{
						fld.InitialValue = defaultValueBytes;
					}
					typeDef.Fields.Add(fld);
					continue;
				}

				if (isStatic)
				{
					typeInfo.DefineStaticField(fieldName, fieldTypeRef, 1);
				}
				else
				{
					typeInfo.DefineField(fieldName, fieldTypeRef, 1);
				}
			}
		}
	}
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Il2CppToolkit.Common;
using Il2CppToolkit.Common.Errors;

namespace Il2CppToolkit.Model
{
	public partial class TypeModel
	{
		private readonly Dictionary<Il2CppMethodDefinition, ulong> methodAddresses = new();
		private readonly Dictionary<Il2CppMethodSpec, ulong> methodSpecAddresses = new();
		private readonly Dictionary<Il2CppTypeDefinition, ulong> m_typeDefToAddress = new();
		private readonly Dictionary<int, TypeDescriptor> m_typeCache = new();
		private readonly List<TypeDescriptor> m_typeDescriptors = new();

		private void IndexTypeDescriptors()
		{
			// already indexed?
			if (m_typeDescriptors.Count > 0)
			{
				return;
			}

			foreach ((uint metadataUsageIndex, uint methodSpecIndex) in Loader.Metadata.metadataUsageDic[
				Il2CppMetadataUsage.kIl2CppMetadataUsageMethodRef])
			{
				Il2CppMethodSpec methodSpec = Loader.Il2Cpp.MethodSpecs[methodSpecIndex];
				ulong address = Loader.Il2Cpp.GetRVA(Loader.Il2Cpp.MetadataUsages[metadataUsageIndex]);
				methodSpecAddresses.Add(methodSpec, address);
			}

			foreach ((uint metadataUsageIndex, uint methodDefIndex) in Loader.Metadata.metadataUsageDic[Il2CppMetadataUsage.kIl2CppMetadataUsageMethodDef])
			{
				Il2CppMethodDefinition methodDef = Loader.Metadata.methodDefs[methodDefIndex];
				ulong address = Loader.Il2Cpp.GetRVA(Loader.Il2Cpp.MetadataUsages[metadataUsageIndex]);
				methodAddresses.Add(methodDef, address);
			}

			foreach ((uint metadataUsageIndex, uint typeIndex) in Loader.Metadata.metadataUsageDic[Il2CppMetadataUsage.kIl2CppMetadataUsageTypeInfo])
			{
				Il2CppType il2CppType = Loader.Il2Cpp.Types[typeIndex];
				Il2CppTypeDefinition typeDef = GetTypeDefinitionFromIl2CppType(il2CppType, false);
				if (typeDef == null) continue;

				ulong address = Loader.Il2Cpp.GetRVA(Loader.Il2Cpp.MetadataUsages[metadataUsageIndex]);
				m_typeDefToAddress.Add(typeDef, address);
			}

			// Declare all types before associating dependencies
			for (int imageIndex = 0; imageIndex < Loader.Metadata.imageDefs.Length; ++imageIndex)
			{
				Il2CppImageDefinition imageDef = Loader.Metadata.imageDefs[imageIndex];
				long typeEnd = imageDef.typeStart + imageDef.typeCount;
				for (int typeDefIndex = imageDef.typeStart; typeDefIndex < typeEnd; typeDefIndex++)
				{
					Il2CppTypeDefinition typeDef = Loader.Metadata.typeDefs[typeDefIndex];
					m_typeDescriptors.Add(MakeTypeDescriptor(typeDef, typeDefIndex, imageDef));
				}
			}

			// Build dependencies
			foreach (TypeDescriptor td in m_typeDescriptors)
			{
				TypeAttributes attribs = Helpers.GetTypeAttributes(td.TypeDef);
				td.Attributes = attribs;

				// nested within type (parent)
				if (td.TypeDef.declaringTypeIndex != -1)
				{
					ErrorHandler.Assert((attribs & TypeAttributes.VisibilityMask) > TypeAttributes.Public, "Nested attribute missing");
					Il2CppType cppType = Loader.Il2Cpp.Types[td.TypeDef.declaringTypeIndex];
					td.DeclaringParent = m_typeCache[(int)cppType.data.klassIndex];
				}
				else
				{
					ErrorHandler.Assert((attribs & TypeAttributes.VisibilityMask) <= TypeAttributes.Public, "Unexpected nested attribute");
				}

				// nested types (children)
				if (td.TypeDef.nested_type_count > 0)
				{
					foreach (int typeIndex in Loader.Metadata.nestedTypeIndices.Range(td.TypeDef.nestedTypesStart, td.TypeDef.nested_type_count))
					{
						td.NestedTypes.Add(m_typeCache[typeIndex]);
					}
				}

				// generic parameters
				if (td.TypeDef.genericContainerIndex != -1)
				{
					Il2CppGenericContainer genericContainer = Loader.Metadata.genericContainers[td.TypeDef.genericContainerIndex];
					td.GenericParameterNames = GetGenericContainerParamNames(genericContainer);
					ErrorHandler.Assert(td.GenericParameterNames.Length > 0, "Generic class must have template arguments");
				}

				// base class
				if (attribs.HasFlag(TypeAttributes.Interface))
				{
					td.Base = null;
				}
				else if (td.TypeDef.IsEnum)
				{
					// TODO: Replace with flag?
					td.Base = new DotNetTypeReference(typeof(Enum));
				}
				else if (td.TypeDef.parentIndex >= 0)
				{
					ITypeReference parentTypeReference = MakeTypeReferenceFromCppTypeIndex(td.TypeDef.parentIndex, td);
					if (parentTypeReference.Name != "System.Object")
					{
						td.Base = parentTypeReference;
					}
				}
				else
				{
					ErrorHandler.Assert(!td.TypeDef.IsValueType, "Unexpected value type");
				}

				// interfaces
				foreach (int interfaceTypeIndex in Loader.Metadata.interfaceIndices.Range(td.TypeDef.interfacesStart, td.TypeDef.interfaces_count))
				{
					td.Implements.Add(MakeTypeReferenceFromCppTypeIndex(interfaceTypeIndex, td));
				}

				// fields
				if (td.TypeDef.field_count > 0)
				{
					foreach ((int fieldIndex, Il2CppFieldDefinition fieldDef) in Loader.Metadata.fieldDefs.RangeWithIndexes(td.TypeDef.fieldStart, td.TypeDef.field_count))
					{
						Il2CppType fieldCppType = Loader.Il2Cpp.Types[fieldDef.typeIndex];
						ITypeReference fieldType = MakeTypeReferenceFromCppTypeIndex(fieldDef.typeIndex, td);
						string fieldName = Loader.Metadata.GetStringFromIndex(fieldDef.nameIndex);
						FieldAttributes attrs = (FieldAttributes)fieldCppType.attrs & ~FieldAttributes.InitOnly;
						ulong offset = GetFieldOffsetFromIndex(td.TypeDef, fieldIndex);
						FieldDescriptor fieldDescriptor = new(fieldName, fieldType, attrs, offset);
						if (Loader.Metadata.GetFieldDefaultValueFromIndex(fieldIndex, out Il2CppFieldDefaultValue fieldDefaultValue) && fieldDefaultValue.dataIndex != -1)
						{
							if (TryGetDefaultValue(fieldDefaultValue.typeIndex, fieldDefaultValue.dataIndex, out object value))
							{
								fieldDescriptor.DefaultValue = value;
							}
						}
						td.Fields.Add(fieldDescriptor);
					}
				}

				// properties
				if (td.TypeDef.property_count > 0)
				{
					foreach (int propertyIndex in Enumerable.Range(td.TypeDef.propertyStart, td.TypeDef.property_count))
					{
						Il2CppPropertyDefinition propertyDef = Loader.Metadata.propertyDefs[propertyIndex];
						if (propertyDef.get < 0) continue;

						Il2CppMethodDefinition methodDef = Loader.Metadata.methodDefs[td.TypeDef.methodStart + propertyDef.get];
						Il2CppType propertyType = Loader.Il2Cpp.Types[methodDef.returnType];
						MethodAttributes getAttribs = (MethodAttributes)methodDef.flags;
						string propertyName = Loader.Metadata.GetStringFromIndex(propertyDef.nameIndex);
						PropertyAttributes attrs = (PropertyAttributes)propertyType.attrs;
						ITypeReference propertyTypeRef = MakeTypeReferenceFromCppTypeIndex(methodDef.returnType, td);
						PropertyDescriptor propertyDescriptor = new(propertyName, propertyTypeRef, attrs, getAttribs);
						td.Properties.Add(propertyDescriptor);
					}
				}

				// methods
				UniqueName uniqueMethodName = new();
				foreach (int methodIndex in Enumerable.Range(td.TypeDef.methodStart, td.TypeDef.method_count))
				{
					Il2CppMethodDefinition methodDef = Loader.Metadata.methodDefs[methodIndex];
					string methodName = uniqueMethodName.Get(Loader.Metadata.GetStringFromIndex(methodDef.nameIndex));
					// only static, non-ctor methods
					if (methodName.StartsWith(".") || !((MethodAttributes)methodDef.flags).HasFlag(MethodAttributes.Static))
					{
						continue;
					}

					// generic instance method arguments
					if (Loader.Il2Cpp.MethodDefinitionMethodSpecs.TryGetValue(methodIndex, out List<Il2CppMethodSpec> MethodSpecs))
					{
						foreach (Il2CppMethodSpec methodSpec in MethodSpecs)
						{
							if (methodSpec.classIndexIndex == -1) continue;
							if (!methodSpecAddresses.TryGetValue(methodSpec, out ulong address)) continue;

							Il2CppGenericInst classInst = Loader.Il2Cpp.GenericInsts[methodSpec.classIndexIndex];
							ulong[] pointers = Loader.Il2Cpp.MapVATR<ulong>(classInst.type_argv, classInst.type_argc);

							MethodDescriptor md = new(methodName, address);
							for (int i = 0; i < classInst.type_argc; i++)
							{
								Il2CppType il2CppType = Loader.Il2Cpp.GetIl2CppType(pointers[i]);
								string typeName = GetTypeName(il2CppType, true, false);
								md.DeclaringTypeArgs.Add(new Il2CppTypeReference(typeName, il2CppType, td));
							}

							td.Methods.Add(md);
						}
					}
					// TODO: Get address
					//else
					//{
					//	MethodDescriptor md = new(methodName, 0);
					//	ulong address = methodAddresses[methodDef];
					//	td.Methods.Add(md, address);
					//}
				}
			}
		}

		private ITypeReference MakeTypeReferenceFromCppTypeIndex(int typeIndex, TypeDescriptor descriptor)
		{
			Il2CppType cppType = Loader.Il2Cpp.Types[typeIndex];
			string baseTypeName = GetTypeName(cppType, addNamespace: true, is_nested: false);
			return new Il2CppTypeReference(baseTypeName, cppType, descriptor);
		}

		private TypeDescriptor MakeTypeDescriptor(Il2CppTypeDefinition typeDef, int typeIndex, Il2CppImageDefinition imageDef)
		{
			string typeName = GetTypeDefName(typeDef);
			TypeDescriptor td = new(typeName, typeDef, typeIndex, imageDef);
			m_typeCache.Add(typeIndex, td);
			return td;
		}

		private string GetTypeDefName(Il2CppTypeDefinition typeDef)
		{
			string typeName = Loader.Metadata.GetStringFromIndex(typeDef.nameIndex);
			int index = typeName.IndexOf("`", StringComparison.Ordinal);
			if (index != -1)
			{
				typeName = typeName[..index];
			}
			string ns = Loader.Metadata.GetStringFromIndex(typeDef.namespaceIndex);
			if (ns != "")
			{
				typeName = ns + "." + typeName;
			}

			return typeName;
		}

		private bool TryGetDefaultValue(int typeIndex, int dataIndex, out object value)
		{
			uint pointer = Loader.Metadata.GetDefaultValueFromIndex(dataIndex);
			Il2CppType defaultValueType = Loader.Il2Cpp.Types[typeIndex];
			Loader.Metadata.Position = pointer;
			switch (defaultValueType.type)
			{
				case Il2CppTypeEnum.IL2CPP_TYPE_BOOLEAN:
					value = Loader.Metadata.ReadBoolean();
					return true;
				case Il2CppTypeEnum.IL2CPP_TYPE_U1:
					value = Loader.Metadata.ReadByte();
					return true;
				case Il2CppTypeEnum.IL2CPP_TYPE_I1:
					value = Loader.Metadata.ReadSByte();
					return true;
				case Il2CppTypeEnum.IL2CPP_TYPE_CHAR:
					value = BitConverter.ToChar(Loader.Metadata.ReadBytes(2), 0);
					return true;
				case Il2CppTypeEnum.IL2CPP_TYPE_U2:
					value = Loader.Metadata.ReadUInt16();
					return true;
				case Il2CppTypeEnum.IL2CPP_TYPE_I2:
					value = Loader.Metadata.ReadInt16();
					return true;
				case Il2CppTypeEnum.IL2CPP_TYPE_U4:
					value = Loader.Metadata.ReadUInt32();
					return true;
				case Il2CppTypeEnum.IL2CPP_TYPE_I4:
					value = Loader.Metadata.ReadInt32();
					return true;
				case Il2CppTypeEnum.IL2CPP_TYPE_U8:
					value = Loader.Metadata.ReadUInt64();
					return true;
				case Il2CppTypeEnum.IL2CPP_TYPE_I8:
					value = Loader.Metadata.ReadInt64();
					return true;
				case Il2CppTypeEnum.IL2CPP_TYPE_R4:
					value = Loader.Metadata.ReadSingle();
					return true;
				case Il2CppTypeEnum.IL2CPP_TYPE_R8:
					value = Loader.Metadata.ReadDouble();
					return true;
				case Il2CppTypeEnum.IL2CPP_TYPE_STRING:
					int len = Loader.Metadata.ReadInt32();
					value = Loader.Metadata.ReadString(len);
					return true;
				default:
					value = pointer;
					return false;
			}
		}
	}
}
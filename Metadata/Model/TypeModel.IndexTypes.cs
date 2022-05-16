using System;
using System.Collections;
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
		private readonly Dictionary<Il2CppType, ulong> m_typeToAddress = new();
		private readonly Dictionary<int, TypeDescriptor> m_parentTypeIndexToTypeInstDescriptor = new();
		private readonly Dictionary<int, TypeDescriptor> m_typeCache = new();
		private Dictionary<Il2CppTypeDefinition, Il2CppGenericClass[]> m_genericClassList = new();
		private readonly List<TypeDescriptor> m_typeDescriptors = new();

		private void IndexTypeDescriptors()
		{
			// already indexed?
			if (m_typeDescriptors.Count > 0)
			{
				return;
			}

			ProcessTypeMetadata();

			// Declare all types before associating dependencies
			for (int imageIndex = 0; imageIndex < m_loader.Metadata.imageDefs.Length; ++imageIndex)
			{
				Il2CppImageDefinition imageDef = m_loader.Metadata.imageDefs[imageIndex];
				long typeEnd = imageDef.typeStart + imageDef.typeCount;
				for (int typeDefIndex = imageDef.typeStart; typeDefIndex < typeEnd; typeDefIndex++)
				{
					Il2CppTypeDefinition typeDef = m_loader.Metadata.typeDefs[typeDefIndex];
					m_typeDescriptors.Add(MakeTypeDescriptor(typeDef, typeDefIndex, imageDef));
				}
			}

			// Build dependencies
			foreach (TypeDescriptor td in m_typeDescriptors)
			{
				TypeAttributes attribs = td.Attributes;

				// nested within type (parent)
				if (td.TypeDef.declaringTypeIndex != -1)
				{
					ErrorHandler.Assert((attribs & TypeAttributes.VisibilityMask) > TypeAttributes.Public, "Nested attribute missing");
					Il2CppType cppType = m_loader.Il2Cpp.Types[td.TypeDef.declaringTypeIndex];
					td.DeclaringParent = m_typeCache[(int)cppType.data.klassIndex];
				}
				else
				{
					ErrorHandler.Assert((attribs & TypeAttributes.VisibilityMask) <= TypeAttributes.Public, "Unexpected nested attribute");
				}

				// nested types (children)
				if (td.TypeDef.nested_type_count > 0)
				{
					foreach (int typeIndex in m_loader.Metadata.nestedTypeIndices.Range(td.TypeDef.nestedTypesStart, td.TypeDef.nested_type_count))
					{
						td.NestedTypes.Add(m_typeCache[typeIndex]);
					}
				}

				// generic parameters
				// TODO: fork based on whether this is a generic inst vs. generic def
				if (td.GenericClass != null) // generic inst
				{
					ITypeReference parentTypeReference = MakeTypeReferenceFromCppTypeIndex((int)td.GenericTypeIndex, td);
					ErrorHandler.Assert(parentTypeReference.Name != "System.Object", "generic class instance must derive from a generic class definition");
					if (parentTypeReference.Name != "System.Object")
					{
						td.Base = parentTypeReference;
					}
				}
				else
				{
					if (td.TypeDef.genericContainerIndex != -1)
					{
						Il2CppGenericContainer genericContainer = m_loader.Metadata.genericContainers[td.TypeDef.genericContainerIndex];
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
						if (m_parentTypeIndexToTypeInstDescriptor.TryGetValue(td.TypeDef.parentIndex, out TypeDescriptor parentInstDescriptor))
						{
							td.Base = new TypeDescriptorReference(parentInstDescriptor);
						}
						else
						{
							ITypeReference parentTypeReference = MakeTypeReferenceFromCppTypeIndex(td.TypeDef.parentIndex, td);
							if (parentTypeReference.Name != "System.Object")
							{
								td.Base = parentTypeReference;
							}
						}
					}
					else
					{
						ErrorHandler.Assert(!td.TypeDef.IsValueType, "Unexpected value type");
					}

					// interfaces
					foreach (int interfaceTypeIndex in m_loader.Metadata.interfaceIndices.Range(td.TypeDef.interfacesStart, td.TypeDef.interfaces_count))
					{
						td.Implements.Add(MakeTypeReferenceFromCppTypeIndex(interfaceTypeIndex, td));
					}
				}

				// fields
				if (td.TypeDef.field_count > 0)
				{
					foreach ((int fieldIndex, Il2CppFieldDefinition fieldDef) in m_loader.Metadata.fieldDefs.RangeWithIndexes(td.TypeDef.fieldStart, td.TypeDef.field_count))
					{
						Il2CppType fieldCppType = m_loader.Il2Cpp.Types[fieldDef.typeIndex];
						ITypeReference fieldType = MakeTypeReferenceFromCppTypeIndex(fieldDef.typeIndex, td);
						string fieldName = m_loader.Metadata.GetStringFromIndex(fieldDef.nameIndex);
						FieldAttributes attrs = (FieldAttributes)fieldCppType.attrs & ~FieldAttributes.InitOnly;
						ulong offset = GetFieldOffsetFromIndex(td.TypeDef, fieldIndex);
						FieldDescriptor fieldDescriptor = new(fieldName, fieldType, attrs, offset);
						if (m_loader.Metadata.GetFieldDefaultValueFromIndex(fieldIndex, out Il2CppFieldDefaultValue fieldDefaultValue) && fieldDefaultValue.dataIndex != -1)
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
						Il2CppPropertyDefinition propertyDef = m_loader.Metadata.propertyDefs[propertyIndex];
						if (propertyDef.get < 0) continue;

						Il2CppMethodDefinition methodDef = m_loader.Metadata.methodDefs[td.TypeDef.methodStart + propertyDef.get];
						Il2CppType propertyType = m_loader.Il2Cpp.Types[methodDef.returnType];
						MethodAttributes getAttribs = (MethodAttributes)methodDef.flags;
						string propertyName = m_loader.Metadata.GetStringFromIndex(propertyDef.nameIndex);
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
					Il2CppMethodDefinition methodDef = m_loader.Metadata.methodDefs[methodIndex];
					string methodName = uniqueMethodName.Get(m_loader.Metadata.GetStringFromIndex(methodDef.nameIndex));
					// only static, non-ctor methods
					if (methodName.StartsWith(".") || !((MethodAttributes)methodDef.flags).HasFlag(MethodAttributes.Static))
					{
						continue;
					}

					// generic instance method arguments
					if (m_loader.Il2Cpp.MethodDefinitionMethodSpecs.TryGetValue(methodIndex, out List<Il2CppMethodSpec> MethodSpecs))
					{
						foreach (Il2CppMethodSpec methodSpec in MethodSpecs)
						{
							if (methodSpec.classIndexIndex == -1) continue;
							if (!methodSpecAddresses.TryGetValue(methodSpec, out ulong address)) continue;

							Il2CppGenericInst classInst = m_loader.Il2Cpp.GenericInsts[methodSpec.classIndexIndex];
							ulong[] pointers = m_loader.Il2Cpp.MapVATR<ulong>(classInst.type_argv, classInst.type_argc);

							MethodDescriptor md = new(methodName, address);
							for (int i = 0; i < classInst.type_argc; i++)
							{
								Il2CppType il2CppType = m_loader.Il2Cpp.GetIl2CppType(pointers[i]);
								string typeName = GetTypeName(il2CppType, true, false);
								md.DeclaringTypeArgs.Add(new Il2CppTypeReference(typeName, il2CppType, td));
							}

							td.Methods.Add(md);
						}
					}
					else
					{
						string imageName = m_loader.Metadata.GetStringFromIndex(td.ImageDef.nameIndex);
						ulong methodPointer = m_loader.Il2Cpp.GetMethodPointer(imageName, methodDef);
						ulong address = m_loader.Il2Cpp.GetRVA(methodPointer);
						MethodDescriptor md = new(methodName, address);
						td.Methods.Add(md);
					}
				}
			}

			// build generic instances map
			m_genericClassList = Il2Cpp.Types
				.Where(type => type.type == Il2CppTypeEnum.IL2CPP_TYPE_GENERICINST)
				.Select(type =>
				{
					ulong pointer = type.data.generic_class;
					var genericClass = Il2Cpp.MapVATR<Il2CppGenericClass>(type.data.generic_class);
					var typeDef = GetGenericClassTypeDefinition(genericClass);
					return new Tuple<Il2CppGenericClass, Il2CppTypeDefinition>(genericClass, typeDef);
				})
				.GroupBy(tuple => tuple.Item2, tuple => tuple.Item1)
				.ToDictionary(group => group.Key, group => group.ToArray());
		}

		private void ProcessTypeMetadata()
		{
			Dictionary<Il2CppMetadataUsage, Action<uint, ulong>> usageHandlers = new()
			{
				{ Il2CppMetadataUsage.kIl2CppMetadataUsageMethodRef, HandleMethodRefUsage },
				{ Il2CppMetadataUsage.kIl2CppMetadataUsageMethodDef, HandleMethodDefUsage },
				{ Il2CppMetadataUsage.kIl2CppMetadataUsageTypeInfo, HandleTypeInfoUsage }
			};

			if (m_loader.Il2Cpp.Version >= 27)
			{
				var sectionHelper = GetSectionHelper();
				foreach (var sec in sectionHelper.data)
				{
					m_loader.Il2Cpp.Position = sec.offset;
					while (m_loader.Il2Cpp.Position < sec.offsetEnd - m_loader.Il2Cpp.PointerSize)
					{
						var addr = m_loader.Il2Cpp.Position;
						var metadataValue = m_loader.Il2Cpp.ReadUIntPtr();
						var position = m_loader.Il2Cpp.Position;
						if (metadataValue < uint.MaxValue)
						{
							var encodedToken = (uint)metadataValue;
							var usage = m_loader.Metadata.GetEncodedIndexType(encodedToken);
							if (usage > 0 && usage <= 6)
							{
								var decodedIndex = m_loader.Metadata.GetDecodedMethodIndex(encodedToken);
								if (metadataValue == ((usage << 29) | (decodedIndex << 1)) + 1)
								{
									if (!usageHandlers.TryGetValue((Il2CppMetadataUsage)usage, out var handler))
										continue;
									var va = m_loader.Il2Cpp.MapRTVA(addr);
									if (va > 0)
									{
										handler(decodedIndex, va);
									}
									if (m_loader.Il2Cpp.Position != position)
									{
										m_loader.Il2Cpp.Position = position;
									}
								}
							}
						}
					}
				}
			}
			else
			{
				foreach (var kvp in usageHandlers)
				{
					foreach (var i in m_loader.Metadata.metadataUsageDic[kvp.Key])
						kvp.Value(i.Value, m_loader.Il2Cpp.MetadataUsages[i.Key]);
				}
			}
		}

		public void HandleMethodRefUsage(uint methodSpecIndex, ulong address)
		{
			if (methodSpecIndex >= m_loader.Il2Cpp.MethodSpecs.Length) return;
			Il2CppMethodSpec methodSpec = m_loader.Il2Cpp.MethodSpecs[methodSpecIndex];
			address = m_loader.Il2Cpp.GetRVA(address);
			methodSpecAddresses.Add(methodSpec, address);
		}

		public void HandleMethodDefUsage(uint methodDefIndex, ulong address)
		{
			if (methodDefIndex >= m_loader.Metadata.methodDefs.Length) return;
			Il2CppMethodDefinition methodDef = m_loader.Metadata.methodDefs[methodDefIndex];
			address = m_loader.Il2Cpp.GetRVA(address);
			methodAddresses[methodDef] = address;
		}

		public void HandleTypeInfoUsage(uint typeIndex, ulong address)
		{
			if (typeIndex >= m_loader.Il2Cpp.Types.Length) return;
			Il2CppType il2CppType = m_loader.Il2Cpp.Types[typeIndex];
			Il2CppTypeDefinition typeDef = GetTypeDefinitionFromIl2CppType(il2CppType, false);
			address = m_loader.Il2Cpp.GetRVA(address);

			m_typeToAddress.Add(il2CppType, address);
			if (il2CppType.type == Il2CppTypeEnum.IL2CPP_TYPE_GENERICINST)
			{
				TypeDescriptor td = MakeGenericTypeInstDescriptor(typeIndex);
				if (td.Attributes.HasFlag(TypeAttributes.Interface)) return;
				m_typeDescriptors.Add(td);
			}
			else if (typeDef != null)
			{
				m_typeDefToAddress.Add(typeDef, address);
			}
		}

		private ITypeReference MakeTypeReferenceFromCppTypeIndex(int typeIndex, TypeDescriptor descriptor)
		{
			Il2CppType cppType = m_loader.Il2Cpp.Types[typeIndex];
			string baseTypeName = GetTypeName(cppType, addNamespace: true, is_nested: false);
			return new Il2CppTypeReference(baseTypeName, cppType, descriptor);
		}

		private TypeDescriptor MakeTypeDescriptor(Il2CppTypeDefinition typeDef, int typeIndex, Il2CppImageDefinition imageDef)
		{
			string typeName = GetTypeDefName(typeDef);
			TypeDescriptor td = new(typeName, typeDef, imageDef);
			td.SizeInBytes = (uint)m_loader.Il2Cpp.TypeDefinitionSizes[typeIndex].instance_size;
			if (m_typeDefToAddress.TryGetValue(typeDef, out ulong address))
			{
				td.TypeInfo = new()
				{
					Address = address,
					ModuleName = ModuleName,
				};
			}

			m_typeCache.Add(typeIndex, td);
			return td;
		}

		private TypeDescriptor MakeGenericTypeInstDescriptor(uint typeIndex)
		{
			Il2CppType il2CppType = m_loader.Il2Cpp.Types[typeIndex];
			ErrorHandler.Assert(il2CppType.type == Il2CppTypeEnum.IL2CPP_TYPE_GENERICINST, "Expected GenericInst type");
			string typeName = GetTypeName(il2CppType, addNamespace: true, is_nested: false);
			Il2CppGenericClass genericClass = m_loader.Il2Cpp.MapVATR<Il2CppGenericClass>(il2CppType.data.generic_class);
			long genericTypeDefIdx = GetGenericClassTypeDefinitionIndex(genericClass);
			ErrorHandler.Assert(genericTypeDefIdx != -1, "Could not find generic type index");
			Il2CppTypeDefinition genericTypeDef = m_loader.Metadata.typeDefs[genericTypeDefIdx];
			TypeDescriptor td = new(typeName, genericTypeDef, null, genericClass, typeIndex);
			td.SizeInBytes = (uint)m_loader.Il2Cpp.TypeDefinitionSizes[genericTypeDefIdx].instance_size;
			if (m_typeToAddress.TryGetValue(il2CppType, out ulong address))
			{
				td.TypeInfo = new()
				{
					Address = address,
					ModuleName = ModuleName,
				};
			}

			m_parentTypeIndexToTypeInstDescriptor[(int)typeIndex] = td;
			return td;
		}

		private string GetTypeDefName(Il2CppTypeDefinition typeDef)
		{
			string typeName = m_loader.Metadata.GetStringFromIndex(typeDef.nameIndex);
			int index = typeName.IndexOf("`", StringComparison.Ordinal);
			if (index != -1)
			{
				typeName = typeName.Substring(0, index);
			}
			string ns = m_loader.Metadata.GetStringFromIndex(typeDef.namespaceIndex);
			if (ns != "")
			{
				typeName = ns + "." + typeName;
			}

			return typeName;
		}

		private bool TryGetDefaultValue(int typeIndex, int dataIndex, out object value)
		{
			uint pointer = m_loader.Metadata.GetDefaultValueFromIndex(dataIndex);
			Il2CppType defaultValueType = m_loader.Il2Cpp.Types[typeIndex];
			m_loader.Metadata.Position = pointer;
			switch (defaultValueType.type)
			{
				case Il2CppTypeEnum.IL2CPP_TYPE_BOOLEAN:
					value = m_loader.Metadata.ReadBoolean();
					return true;
				case Il2CppTypeEnum.IL2CPP_TYPE_U1:
					value = m_loader.Metadata.ReadByte();
					return true;
				case Il2CppTypeEnum.IL2CPP_TYPE_I1:
					value = m_loader.Metadata.ReadSByte();
					return true;
				case Il2CppTypeEnum.IL2CPP_TYPE_CHAR:
					value = BitConverter.ToChar(m_loader.Metadata.ReadBytes(2), 0);
					return true;
				case Il2CppTypeEnum.IL2CPP_TYPE_U2:
					value = m_loader.Metadata.ReadUInt16();
					return true;
				case Il2CppTypeEnum.IL2CPP_TYPE_I2:
					value = m_loader.Metadata.ReadInt16();
					return true;
				case Il2CppTypeEnum.IL2CPP_TYPE_U4:
					value = m_loader.Metadata.ReadUInt32();
					return true;
				case Il2CppTypeEnum.IL2CPP_TYPE_I4:
					value = m_loader.Metadata.ReadInt32();
					return true;
				case Il2CppTypeEnum.IL2CPP_TYPE_U8:
					value = m_loader.Metadata.ReadUInt64();
					return true;
				case Il2CppTypeEnum.IL2CPP_TYPE_I8:
					value = m_loader.Metadata.ReadInt64();
					return true;
				case Il2CppTypeEnum.IL2CPP_TYPE_R4:
					value = m_loader.Metadata.ReadSingle();
					return true;
				case Il2CppTypeEnum.IL2CPP_TYPE_R8:
					value = m_loader.Metadata.ReadDouble();
					return true;
				case Il2CppTypeEnum.IL2CPP_TYPE_STRING:
					int len = m_loader.Metadata.ReadInt32();
					value = m_loader.Metadata.ReadString(len);
					return true;
				default:
					value = pointer;
					return false;
			}
		}
	}
}
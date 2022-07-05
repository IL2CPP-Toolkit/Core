using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Il2CppToolkit.Model
{
	public partial class TypeModel
	{
		private static readonly Dictionary<int, string> TypeString = new Dictionary<int, string>
		{
			{1,typeof(void).FullName},
			{2,typeof(bool).FullName},
			{3,typeof(char).FullName},
			{4,typeof(sbyte).FullName},
			{5,typeof(byte).FullName},
			{6,typeof(short).FullName},
			{7,typeof(ushort).FullName},
			{8,typeof(int).FullName},
			{9,typeof(uint).FullName},
			{10,typeof(long).FullName},
			{11,typeof(ulong).FullName},
			{12,typeof(float).FullName},
			{13,typeof(double).FullName},
			{14,typeof(string).FullName},
			{22,typeof(IntPtr).FullName},
			{24,typeof(IntPtr).FullName},
			{25,typeof(UIntPtr).FullName},
			{28,typeof(object).FullName},
		};

		public ulong GetFieldOffsetFromIndex(Il2CppTypeDefinition typeDefinition, int fieldIndex)
		{
			ulong offset = m_fieldOffsets[fieldIndex];
			if (offset > 0)
			{
				if (typeDefinition.IsValueType && !((TypeAttributes)typeDefinition.flags).HasFlag(TypeAttributes.Sealed | TypeAttributes.Abstract))
				{
					if (m_loader.Il2Cpp.Is32Bit)
					{
						offset -= 8;
					}
					else
					{
						offset -= 16;
					}
				}
			}
			return offset;
		}

		public string GetTypeName(Il2CppType il2CppType, bool addNamespace, bool is_nested)
		{
			switch (il2CppType.type)
			{
				case Il2CppTypeEnum.IL2CPP_TYPE_ARRAY:
					{
						Il2CppArrayType arrayType = m_loader.Il2Cpp.MapVATR<Il2CppArrayType>(il2CppType.data.array);
						Il2CppType elementType = m_loader.Il2Cpp.GetIl2CppType(arrayType.etype);
						return $"{GetTypeName(elementType, addNamespace, false)}[{new string(',', arrayType.rank - 1)}]";
					}
				case Il2CppTypeEnum.IL2CPP_TYPE_SZARRAY:
					{
						Il2CppType elementType = m_loader.Il2Cpp.GetIl2CppType(il2CppType.data.type);
						return $"{GetTypeName(elementType, addNamespace, false)}[]";
					}
				case Il2CppTypeEnum.IL2CPP_TYPE_PTR:
					{
						Il2CppType oriType = m_loader.Il2Cpp.GetIl2CppType(il2CppType.data.type);
						return $"{GetTypeName(oriType, addNamespace, false)}*";
					}
				case Il2CppTypeEnum.IL2CPP_TYPE_VAR:
				case Il2CppTypeEnum.IL2CPP_TYPE_MVAR:
					{
						Il2CppGenericParameter param = GetGenericParameterFromIl2CppType(il2CppType);
						return m_loader.Metadata.GetStringFromIndex(param.nameIndex);
					}
				case Il2CppTypeEnum.IL2CPP_TYPE_CLASS:
				case Il2CppTypeEnum.IL2CPP_TYPE_VALUETYPE:
				case Il2CppTypeEnum.IL2CPP_TYPE_GENERICINST:
					{
						string str = string.Empty;
						Il2CppTypeDefinition typeDef;
						Il2CppGenericClass genericClass = null;
						if (il2CppType.type == Il2CppTypeEnum.IL2CPP_TYPE_GENERICINST)
						{
							genericClass = m_loader.Il2Cpp.MapVATR<Il2CppGenericClass>(il2CppType.data.generic_class);
							typeDef = GetGenericClassTypeDefinition(genericClass);
						}
						else
						{
							typeDef = GetTypeDefinitionFromIl2CppType(il2CppType);
						}
						if (typeDef.declaringTypeIndex != -1)
						{
							str += GetTypeName(m_loader.Il2Cpp.Types[typeDef.declaringTypeIndex], addNamespace, true);
							str += '.';
						}
						else if (addNamespace)
						{
							string @namespace = m_loader.Metadata.GetStringFromIndex(typeDef.namespaceIndex);
							if (@namespace != "")
							{
								str += @namespace + ".";
							}
						}

						string typeName = m_loader.Metadata.GetStringFromIndex(typeDef.nameIndex);
						int index = typeName.IndexOf("`");
						if (index != -1)
						{
							str += typeName.Substring(0, index);
						}
						else
						{
							str += typeName;
						}

						if (is_nested)
						{
							return str;
						}

						if (genericClass != null)
						{
							Il2CppGenericInst genericInst = m_loader.Il2Cpp.MapVATR<Il2CppGenericInst>(genericClass.context.class_inst);
							str += GetGenericInstParams(genericInst);
						}
						else if (typeDef.genericContainerIndex >= 0)
						{
							Il2CppGenericContainer genericContainer = m_loader.Metadata.genericContainers[typeDef.genericContainerIndex];
							str += GetGenericContainerParams(genericContainer);
						}

						return str;
					}
				default:
					return TypeString[(int)il2CppType.type];
			}
		}

		public long GetGenericClassTypeDefinitionIndex(Il2CppGenericClass genericClass)
		{
			if (m_loader.Il2Cpp.Version >= 27)
			{
				Il2CppType il2CppType = m_loader.Il2Cpp.GetIl2CppType(genericClass.type);
				return GetTypeDefinitionIndexFromIl2CppType(il2CppType);
			}
			if (genericClass.typeDefinitionIndex == 4294967295 || genericClass.typeDefinitionIndex == -1)
			{
				return -1;
			}
			return genericClass.typeDefinitionIndex;
		}

		public Il2CppTypeDefinition GetGenericClassTypeDefinition(Il2CppGenericClass genericClass)
		{
			long index = GetGenericClassTypeDefinitionIndex(genericClass);
			if (index == -1)
				return null;
			return m_loader.Metadata.typeDefs[index];
		}

		public Il2CppGenericParameter GetGenericParameterFromIl2CppType(Il2CppType il2CppType)
		{
			if (m_loader.Il2Cpp.Version >= 27 && m_loader.Il2Cpp is ElfBase elf && elf.IsDumped)
			{
				ulong offset = il2CppType.data.genericParameterHandle - m_loader.Metadata.Address - m_loader.Metadata.header.genericParametersOffset;
				ulong index = offset / (ulong)m_loader.Metadata.SizeOf(typeof(Il2CppGenericParameter));
				return m_loader.Metadata.genericParameters[index];
			}
			else
			{
				return m_loader.Metadata.genericParameters[il2CppType.data.genericParameterIndex];
			}
		}

		public long GetTypeDefinitionIndexFromIl2CppType(Il2CppType il2CppType, bool resolveGeneric = true)
		{
			if (m_loader.Il2Cpp.Version >= 27 && m_loader.Il2Cpp is ElfBase elf && elf.IsDumped)
			{
				ulong offset = il2CppType.data.typeHandle - m_loader.Metadata.Address - m_loader.Metadata.header.typeDefinitionsOffset;
				ulong index = offset / (ulong)m_loader.Metadata.SizeOf(typeof(Il2CppTypeDefinition));
				return (long)index;
			}
			else
			{
				if (il2CppType.type == Il2CppTypeEnum.IL2CPP_TYPE_GENERICINST && resolveGeneric)
				{
					Il2CppGenericClass genericClass = m_loader.Il2Cpp.MapVATR<Il2CppGenericClass>(il2CppType.data.generic_class);
					return GetGenericClassTypeDefinitionIndex(genericClass);
				}
				if (il2CppType.data.klassIndex < m_loader.Metadata.typeDefs.Length)
				{
					return il2CppType.data.klassIndex;
				}
				return -1;
			}
		}

		public Il2CppTypeDefinition GetTypeDefinitionFromIl2CppType(Il2CppType il2CppType, bool resolveGeneric = true)
		{
			long index = GetTypeDefinitionIndexFromIl2CppType(il2CppType, resolveGeneric);
			if (index == -1)
				return null;
			return m_loader.Metadata.typeDefs[index];
		}

		public string GetGenericInstParams(Il2CppGenericInst genericInst)
		{
			List<string> genericParameterNames = new();
			ulong[] pointers = m_loader.Il2Cpp.MapVATR<ulong>(genericInst.type_argv, genericInst.type_argc);
			for (int i = 0; i < genericInst.type_argc; i++)
			{
				Il2CppType il2CppType = m_loader.Il2Cpp.GetIl2CppType(pointers[i]);
				genericParameterNames.Add(GetTypeName(il2CppType, false, false));
			}
			return $"<{string.Join(", ", genericParameterNames)}>";
		}

		public Il2CppType[] GetGenericInstParamList(Il2CppGenericInst genericInst)
		{
			Il2CppType[] genericParameterTypes = new Il2CppType[genericInst.type_argc];
			ulong[] pointers = m_loader.Il2Cpp.MapVATR<ulong>(genericInst.type_argv, genericInst.type_argc);
			for (int i = 0; i < genericInst.type_argc; i++)
			{
				Il2CppType il2CppType = m_loader.Il2Cpp.GetIl2CppType(pointers[i]);
				genericParameterTypes[i] = il2CppType;
			}
			return genericParameterTypes;
		}

		public string[] GetGenericContainerParamNames(Il2CppGenericContainer genericContainer)
		{
			string[] genericParameterNames = new string[genericContainer.type_argc];
			for (int i = 0; i < genericContainer.type_argc; i++)
			{
				int genericParameterIndex = genericContainer.genericParameterStart + i;
				Il2CppGenericParameter genericParameter = m_loader.Metadata.genericParameters[genericParameterIndex];
				genericParameterNames[i] = m_loader.Metadata.GetStringFromIndex(genericParameter.nameIndex);
			}
			return genericParameterNames;
		}

		public string GetGenericContainerParams(Il2CppGenericContainer genericContainer)
		{
			List<string> genericParameterNames = new List<string>();
			for (int i = 0; i < genericContainer.type_argc; i++)
			{
				int genericParameterIndex = genericContainer.genericParameterStart + i;
				Il2CppGenericParameter genericParameter = m_loader.Metadata.genericParameters[genericParameterIndex];
				genericParameterNames.Add(m_loader.Metadata.GetStringFromIndex(genericParameter.nameIndex));
			}
			return $"<{string.Join(", ", genericParameterNames)}>";
		}

		public SectionHelper GetSectionHelper()
		{
			return m_loader.Il2Cpp.GetSectionHelper(
				m_loader.Metadata.methodDefs.Count(x => x.methodIndex >= 0),
				m_loader.Metadata.typeDefs.Length,
				m_loader.Metadata.imageDefs.Length);
		}
	}
}
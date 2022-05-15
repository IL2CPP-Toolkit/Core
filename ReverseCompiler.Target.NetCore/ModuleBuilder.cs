using System;
using System.Collections.Generic;
using System.Diagnostics;
using Il2CppToolkit.Model;
using Mono.Cecil;

namespace Il2CppToolkit.ReverseCompiler.Target.NetCore
{
	public class ModuleBuilder
	{
		private readonly ICompileContext Context;
		private readonly AssemblyDefinition AssemblyDefinition;
		private readonly Dictionary<Il2CppTypeDefinition, TypeDefinition> TypeDefinitions = new();
		private readonly Dictionary<Il2CppGenericParameter, GenericParameter> GenericParameters = new();
		private readonly Dictionary<Il2CppFieldDefinition, FieldDefinition> Fields = new();
		private readonly Dictionary<Il2CppPropertyDefinition, PropertyDefinition> Properties = new();
		private readonly Dictionary<Il2CppMethodDefinition, MethodDefinition> Methods = new();
		private readonly Dictionary<Il2CppTypeEnum, TypeReference> BuiltInTypes = new();

		private readonly HashSet<Il2CppTypeDefinition> EnqueuedTypes = new();
		private readonly Queue<Il2CppTypeDefinition> TypeDefinitionQueue = new();

		private Il2Cpp Il2Cpp => Context.Model.Il2Cpp;
		private Metadata Metadata => Context.Model.Metadata;

		public ModuleBuilder(ICompileContext context, AssemblyDefinition assemblyDefinition)
		{
			Context = context;
			AssemblyDefinition = assemblyDefinition;
			AddBuiltInTypes(AssemblyDefinition.MainModule);
		}

		public void IncludeTypeDefinition(Il2CppTypeDefinition cppTypeDef)
		{
			UseTypeDefinition(cppTypeDef);
		}

		public void Build()
		{
			BuildDefinitionQueue();
		}

		private TypeDefinition UseTypeDefinition(Il2CppTypeDefinition cppTypeDef)
		{
			TypeDefinition typeDef = GetTypeDefinition(cppTypeDef);
			if (EnqueuedTypes.Contains(cppTypeDef))
				return typeDef;

			if (cppTypeDef.declaringTypeIndex == -1)
				AssemblyDefinition.MainModule.Types.Add(typeDef);

			EnqueuedTypes.Add(cppTypeDef);
			TypeDefinitionQueue.Enqueue(cppTypeDef);
			return typeDef;
		}

		private void BuildDefinitionQueue()
		{
			while (TypeDefinitionQueue.TryDequeue(out Il2CppTypeDefinition cppTypeDef))
			{
				TypeDefinition typeDef = GetTypeDefinition(cppTypeDef);

				// declaring type
				if (cppTypeDef.declaringTypeIndex >= 0)
				{
					Il2CppTypeDefinition declaringType = Context.Model.GetTypeDefinitionFromIl2CppType(Il2Cpp.Types[cppTypeDef.declaringTypeIndex]);
					Debug.Assert(declaringType != null);
					if (declaringType != null)
						UseTypeDefinition(declaringType);
				}

				// nested types
				for (int i = 0; i < cppTypeDef.nested_type_count; i++)
				{
					var nestedIndex = Metadata.nestedTypeIndices[cppTypeDef.nestedTypesStart + i];
					var nestedTypeDef = Metadata.typeDefs[nestedIndex];
					var nestedTypeDefinition = UseTypeDefinition(nestedTypeDef);
					typeDef.NestedTypes.Add(nestedTypeDefinition);
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


		private FieldDefinition GetFieldDefinition(Il2CppFieldDefinition cppFieldDef, TypeDefinition typeDef)
		{
			if (Fields.TryGetValue(cppFieldDef, out FieldDefinition fieldDef))
				return fieldDef;

			Il2CppType cppFieldType = Il2Cpp.Types[cppFieldDef.typeIndex];
			string fieldName = Metadata.GetStringFromIndex(cppFieldDef.nameIndex);
			TypeReference fieldTypeRef = UseTypeReference(typeDef, cppFieldType);

			fieldDef = new FieldDefinition(fieldName, (FieldAttributes)cppFieldType.attrs, fieldTypeRef);
			Fields.Add(cppFieldDef, fieldDef);
			return fieldDef;
		}

		private MethodDefinition GetMethodDefinition(Il2CppMethodDefinition cppMethodDef)
		{
			if (Methods.TryGetValue(cppMethodDef, out MethodDefinition methodDef))
				return methodDef;

			string methodName = Metadata.GetStringFromIndex(cppMethodDef.nameIndex);

			// TODO: Give it a real return type!
			methodDef = new MethodDefinition(methodName, (MethodAttributes)cppMethodDef.flags, AssemblyDefinition.MainModule.TypeSystem.Void);
			Methods.Add(cppMethodDef, methodDef);
			return methodDef;
		}

		private PropertyDefinition GetPropertyDefinition(Il2CppPropertyDefinition cppPropertyDef, Il2CppTypeDefinition cppTypeDef)
		{
			if (Properties.TryGetValue(cppPropertyDef, out PropertyDefinition propertyDef))
				return propertyDef;

			string propertyName = Metadata.GetStringFromIndex(cppPropertyDef.nameIndex);
			TypeReference propertyType = null;
			if (cppPropertyDef.get >= 0)
			{
				MethodDefinition methodDef = GetMethodDefinition(Metadata.methodDefs[cppTypeDef.methodStart + cppPropertyDef.get]);
				propertyType = methodDef.ReturnType;
			}
			else if (cppPropertyDef.set >= 0)
			{
				MethodDefinition methodDef = GetMethodDefinition(Metadata.methodDefs[cppTypeDef.methodStart + cppPropertyDef.set]);
				propertyType = methodDef.Parameters[0].ParameterType;
			}

			propertyDef = new PropertyDefinition(propertyName, (PropertyAttributes)cppPropertyDef.attrs, propertyType);
			Properties.Add(cppPropertyDef, propertyDef);
			return propertyDef;
		}

		private TypeDefinition GetTypeDefinition(Il2CppTypeDefinition cppTypeDef)
		{
			if (TypeDefinitions.TryGetValue(cppTypeDef, out TypeDefinition typeDef))
				return typeDef;

			string namespaceName = Metadata.GetStringFromIndex(cppTypeDef.namespaceIndex);
			string typeName = Metadata.GetStringFromIndex(cppTypeDef.nameIndex);

			typeDef = new TypeDefinition(namespaceName, typeName, (TypeAttributes)cppTypeDef.flags);
			TypeDefinitions.Add(cppTypeDef, typeDef);
			return typeDef;
		}

		private TypeReference UseTypeReference(MemberReference memberReference, Il2CppType cppType)
		{
			if (BuiltInTypes.TryGetValue(cppType.type, out TypeReference typeRef))
				return typeRef;

			ModuleDefinition moduleDefinition = AssemblyDefinition.MainModule;
			switch (cppType.type)
			{
				case Il2CppTypeEnum.IL2CPP_TYPE_CLASS:
				case Il2CppTypeEnum.IL2CPP_TYPE_VALUETYPE:
					{
						Il2CppTypeDefinition cppTypeDef = Context.Model.GetTypeDefinitionFromIl2CppType(cppType);
						TypeDefinition typeDef = UseTypeDefinition(cppTypeDef);
						return moduleDefinition.ImportReference(typeDef);
					}
				case Il2CppTypeEnum.IL2CPP_TYPE_ARRAY:
					{
						Il2CppArrayType cppArrayType = Context.Model.Il2Cpp.MapVATR<Il2CppArrayType>(cppType.data.array);
						Il2CppType cppElementType = Context.Model.Il2Cpp.GetIl2CppType(cppArrayType.etype);
						return new ArrayType(UseTypeReference(memberReference, cppElementType), cppArrayType.rank);
					}
				case Il2CppTypeEnum.IL2CPP_TYPE_GENERICINST:
					{
						Il2CppGenericClass cppGenericClass = Context.Model.Il2Cpp.MapVATR<Il2CppGenericClass>(cppType.data.generic_class);
						Il2CppTypeDefinition cppTypeDef = Context.Model.GetGenericClassTypeDefinition(cppGenericClass);
						TypeDefinition typeDef = UseTypeDefinition(cppTypeDef);
						GenericInstanceType genericInstanceType = new(moduleDefinition.ImportReference(typeDef));
						Il2CppGenericInst cppGenericInst = Context.Model.Il2Cpp.MapVATR<Il2CppGenericInst>(cppGenericClass.context.class_inst);
						ulong[] pointers = Context.Model.Il2Cpp.MapVATR<ulong>(cppGenericInst.type_argv, cppGenericInst.type_argc);
						foreach (ulong pointer in pointers)
						{
							Il2CppType cppArgType = Context.Model.Il2Cpp.GetIl2CppType(pointer);
							genericInstanceType.GenericArguments.Add(UseTypeReference(memberReference, cppArgType));
						}
						return genericInstanceType;
					}
				case Il2CppTypeEnum.IL2CPP_TYPE_SZARRAY:
					{
						Il2CppType cppElementType = Context.Model.Il2Cpp.GetIl2CppType(cppType.data.type);
						return new ArrayType(UseTypeReference(memberReference, cppElementType));
					}
				case Il2CppTypeEnum.IL2CPP_TYPE_VAR:
					{
						return memberReference switch
						{
							MethodDefinition methodDefinition => CreateGenericParameter(Context.Model.GetGenericParameterFromIl2CppType(cppType), methodDefinition.DeclaringType),
							TypeDefinition typeDefinition => CreateGenericParameter(Context.Model.GetGenericParameterFromIl2CppType(cppType), typeDefinition),
							_ => throw new NotSupportedException()
						};
					}
				case Il2CppTypeEnum.IL2CPP_TYPE_MVAR:
					{
						if (memberReference is not MethodDefinition methodDefinition)
							throw new NotSupportedException();
						return CreateGenericParameter(Context.Model.GetGenericParameterFromIl2CppType(cppType), methodDefinition);
					}
				case Il2CppTypeEnum.IL2CPP_TYPE_PTR:
					{
						Il2CppType cppElementType = Context.Model.Il2Cpp.GetIl2CppType(cppType.data.type);
						return new PointerType(UseTypeReference(memberReference, cppElementType));
					}
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private GenericParameter CreateGenericParameter(Il2CppGenericParameter param, IGenericParameterProvider iGenericParameterProvider)
		{
			if (!GenericParameters.TryGetValue(param, out GenericParameter genericParameter))
			{
				string genericName = Context.Model.Metadata.GetStringFromIndex(param.nameIndex);
				genericParameter = new(genericName, iGenericParameterProvider)
				{
					Attributes = (GenericParameterAttributes)param.flags
				};
				GenericParameters.Add(param, genericParameter);
				for (int i = 0; i < param.constraintsCount; ++i)
				{
					Il2CppType cppConstraintType = Context.Model.Il2Cpp.Types[Context.Model.Metadata.constraintIndices[param.constraintsStart + i]];
					genericParameter.Constraints.Add(new GenericParameterConstraint(UseTypeReference((MemberReference)iGenericParameterProvider, cppConstraintType)));
				}
			}
			return genericParameter;
		}

		private void AddBuiltInTypes(ModuleDefinition moduleDef)
		{
			BuiltInTypes.Add(Il2CppTypeEnum.IL2CPP_TYPE_OBJECT, moduleDef.TypeSystem.Object);
			BuiltInTypes.Add(Il2CppTypeEnum.IL2CPP_TYPE_VOID, moduleDef.TypeSystem.Void);
			BuiltInTypes.Add(Il2CppTypeEnum.IL2CPP_TYPE_BOOLEAN, moduleDef.TypeSystem.Boolean);
			BuiltInTypes.Add(Il2CppTypeEnum.IL2CPP_TYPE_CHAR, moduleDef.TypeSystem.Char);
			BuiltInTypes.Add(Il2CppTypeEnum.IL2CPP_TYPE_I1, moduleDef.TypeSystem.SByte);
			BuiltInTypes.Add(Il2CppTypeEnum.IL2CPP_TYPE_U1, moduleDef.TypeSystem.Byte);
			BuiltInTypes.Add(Il2CppTypeEnum.IL2CPP_TYPE_I2, moduleDef.TypeSystem.Int16);
			BuiltInTypes.Add(Il2CppTypeEnum.IL2CPP_TYPE_U2, moduleDef.TypeSystem.UInt16);
			BuiltInTypes.Add(Il2CppTypeEnum.IL2CPP_TYPE_I4, moduleDef.TypeSystem.Int32);
			BuiltInTypes.Add(Il2CppTypeEnum.IL2CPP_TYPE_U4, moduleDef.TypeSystem.UInt32);
			BuiltInTypes.Add(Il2CppTypeEnum.IL2CPP_TYPE_I, moduleDef.TypeSystem.IntPtr);
			BuiltInTypes.Add(Il2CppTypeEnum.IL2CPP_TYPE_U, moduleDef.TypeSystem.UIntPtr);
			BuiltInTypes.Add(Il2CppTypeEnum.IL2CPP_TYPE_I8, moduleDef.TypeSystem.Int64);
			BuiltInTypes.Add(Il2CppTypeEnum.IL2CPP_TYPE_U8, moduleDef.TypeSystem.UInt64);
			BuiltInTypes.Add(Il2CppTypeEnum.IL2CPP_TYPE_R4, moduleDef.ImportReference(typeof(float)));
			BuiltInTypes.Add(Il2CppTypeEnum.IL2CPP_TYPE_R8, moduleDef.TypeSystem.Double);
			BuiltInTypes.Add(Il2CppTypeEnum.IL2CPP_TYPE_STRING, moduleDef.TypeSystem.String);
			BuiltInTypes.Add(Il2CppTypeEnum.IL2CPP_TYPE_TYPEDBYREF, moduleDef.ImportReference(typeof(TypedReference)));
		}
	}
}
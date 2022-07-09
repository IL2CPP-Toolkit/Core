using System;
using System.Collections.Generic;
using System.Linq;
using Il2CppToolkit.Model;
using Mono.Cecil;

namespace Il2CppToolkit.ReverseCompiler.Target.NetCore
{
	public partial class ModuleBuilder
	{
		private readonly Dictionary<Type, TypeReference> ImportedTypes = new();
		private readonly Dictionary<Il2CppTypeEnum, TypeReference> BuiltInTypes = new();
		private readonly Dictionary<Il2CppGenericParameter, GenericParameter> GenericParameters = new();
		private readonly Dictionary<Il2CppTypeDefinition, TypeDefinition> TypeDefinitions = new();

		internal TypeReference ImportReference(Type type)
		{
			if (type == null)
				return null;

			if (ImportedTypes.TryGetValue(type, out TypeReference typeRef))
				return typeRef;

			if (SystemRuntimeRef != null && type.Assembly == typeof(string).Assembly)
			{
				typeRef = Module.ImportReference(new TypeReference(type.Namespace, type.Name, null, SystemRuntimeRef, type.IsValueType));
				if (type.ContainsGenericParameters)
				{
					typeRef.GenericParameters.AddRange(type.GetGenericArguments().Select(_ => new GenericParameter(typeRef)));
				}
			}
			else
			{
				typeRef = Module.ImportReference(type);
			}

			ImportedTypes.Add(type, typeRef);
			return typeRef;
		}

		public void IncludeTypeDefinition(Il2CppTypeDefinition cppTypeDef)
		{
			UseTypeDefinition(cppTypeDef);
		}

		private TypeReference UseTypeDefinition(Il2CppTypeDefinition cppTypeDef)
		{
			TypeReference typeDef = GetTypeDefinition(cppTypeDef);
			if (typeDef == null)
				return null;

			if (EnqueuedTypes.Contains(cppTypeDef))
				return typeDef;

			if (cppTypeDef.declaringTypeIndex == -1 && typeDef is TypeDefinition td)
			{
				Module.Types.Add(td);
			}

			EnqueuedTypes.Add(cppTypeDef);
			TypeDefinitionQueue.Enqueue(cppTypeDef);
			return typeDef;
		}

		private TypeReference GetTypeDefinition(Il2CppTypeDefinition cppTypeDef)
		{
			if (TypeDefinitions.TryGetValue(cppTypeDef, out TypeDefinition typeDef))
				return typeDef;

			string typeName = Metadata.GetStringFromIndex(cppTypeDef.nameIndex);
			string fullTypeName = string.Empty;
			// nested type
			if (cppTypeDef.declaringTypeIndex != -1)
			{
				Il2CppTypeDefinition cppDeclaringTypeDef = Context.Model.GetTypeDefinitionFromIl2CppType(Il2Cpp.Types[cppTypeDef.declaringTypeIndex]);
				// System. nested-types cannot be returned
				if (Metadata.GetStringFromIndex(cppDeclaringTypeDef.namespaceIndex).StartsWith("System"))
					return null;

				TypeReference parentRef = GetTypeDefinition(cppDeclaringTypeDef);
				if (parentRef == null || parentRef.Namespace.StartsWith("System"))
					return null;
				fullTypeName = @$"{parentRef.FullName}\";
			}

			string namespaceName = Metadata.GetStringFromIndex(cppTypeDef.namespaceIndex);
			if (!string.IsNullOrEmpty(namespaceName))
				fullTypeName += $"{namespaceName}.";

			fullTypeName += typeName;
			if (Runtime.Types.TypeSystem.TryGetSubstituteType(fullTypeName, out Type mappedType))
			{
				return ImportReference(mappedType);
			}

			typeDef = new TypeDefinition(namespaceName, fullTypeName, (TypeAttributes)cppTypeDef.flags);
			TypeDefinitions.Add(cppTypeDef, typeDef);
			return typeDef;
		}

		internal TypeReference UseTypeReference(MemberReference memberReference, Il2CppType cppType)
		{
			if (BuiltInTypes.TryGetValue(cppType.type, out TypeReference typeRef))
				return typeRef;

			switch (cppType.type)
			{
				case Il2CppTypeEnum.IL2CPP_TYPE_CLASS:
				case Il2CppTypeEnum.IL2CPP_TYPE_VALUETYPE:
					{
						Il2CppTypeDefinition cppTypeDef = Context.Model.GetTypeDefinitionFromIl2CppType(cppType);
						return UseTypeDefinition(cppTypeDef);
					}
				case Il2CppTypeEnum.IL2CPP_TYPE_ARRAY:
					{
						Il2CppArrayType cppArrayType = Context.Model.Il2Cpp.MapVATR<Il2CppArrayType>(cppType.data.array);
						Il2CppType cppElementType = Context.Model.Il2Cpp.GetIl2CppType(cppArrayType.etype);
						typeRef = UseTypeReference(memberReference, cppElementType);
						if (typeRef == null)
							return null;
						return new ArrayType(typeRef, cppArrayType.rank);
					}
				case Il2CppTypeEnum.IL2CPP_TYPE_GENERICINST:
					{
						Il2CppGenericClass cppGenericClass = Context.Model.Il2Cpp.MapVATR<Il2CppGenericClass>(cppType.data.generic_class);
						Il2CppTypeDefinition cppTypeDef = Context.Model.GetGenericClassTypeDefinition(cppGenericClass);
						TypeReference typeDef = UseTypeDefinition(cppTypeDef);
						if (typeDef == null)
							return null;

						GenericInstanceType genericInstanceType = new(typeDef);
						Il2CppGenericInst cppGenericInst = Context.Model.Il2Cpp.MapVATR<Il2CppGenericInst>(cppGenericClass.context.class_inst);
						ulong[] pointers = Context.Model.Il2Cpp.MapVATR<ulong>(cppGenericInst.type_argv, cppGenericInst.type_argc);
						foreach (ulong pointer in pointers)
						{
							Il2CppType cppArgType = Context.Model.Il2Cpp.GetIl2CppType(pointer);
							typeRef = UseTypeReference(memberReference, cppArgType);
							if (typeRef == null)
								return null;
							genericInstanceType.GenericArguments.Add(typeRef);
						}
						return genericInstanceType;
					}
				case Il2CppTypeEnum.IL2CPP_TYPE_SZARRAY:
					{
						Il2CppType cppElementType = Context.Model.Il2Cpp.GetIl2CppType(cppType.data.type);
						typeRef = UseTypeReference(memberReference, cppElementType);
						if (typeRef == null)
							return null;
						return new ArrayType(typeRef);
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
						typeRef = UseTypeReference(memberReference, cppElementType);
						if (typeRef == null)
							return null;
						return new PointerType(typeRef);
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
					TypeReference typeRef = UseTypeReference((MemberReference)iGenericParameterProvider, cppConstraintType);
					if (typeRef == null)
					{
						Context.Logger?.LogWarning($"Unsupported constraint");
						continue;
					}
					genericParameter.Constraints.Add(new GenericParameterConstraint(typeRef));
				}
			}
			return genericParameter;
		}

		private void AddBuiltInTypes(ModuleDefinition moduleDef)
		{
			BuiltInTypes.Add(Il2CppTypeEnum.IL2CPP_TYPE_OBJECT, ImportReference(typeof(Object)));
			BuiltInTypes.Add(Il2CppTypeEnum.IL2CPP_TYPE_VOID, ImportReference(typeof(void)));
			BuiltInTypes.Add(Il2CppTypeEnum.IL2CPP_TYPE_BOOLEAN, ImportReference(typeof(Boolean)));
			BuiltInTypes.Add(Il2CppTypeEnum.IL2CPP_TYPE_CHAR, ImportReference(typeof(Char)));
			BuiltInTypes.Add(Il2CppTypeEnum.IL2CPP_TYPE_I1, ImportReference(typeof(SByte)));
			BuiltInTypes.Add(Il2CppTypeEnum.IL2CPP_TYPE_U1, ImportReference(typeof(Byte)));
			BuiltInTypes.Add(Il2CppTypeEnum.IL2CPP_TYPE_I2, ImportReference(typeof(Int16)));
			BuiltInTypes.Add(Il2CppTypeEnum.IL2CPP_TYPE_U2, ImportReference(typeof(UInt16)));
			BuiltInTypes.Add(Il2CppTypeEnum.IL2CPP_TYPE_I4, ImportReference(typeof(Int32)));
			BuiltInTypes.Add(Il2CppTypeEnum.IL2CPP_TYPE_U4, ImportReference(typeof(UInt32)));
			BuiltInTypes.Add(Il2CppTypeEnum.IL2CPP_TYPE_I, ImportReference(typeof(IntPtr)));
			BuiltInTypes.Add(Il2CppTypeEnum.IL2CPP_TYPE_U, ImportReference(typeof(UIntPtr)));
			BuiltInTypes.Add(Il2CppTypeEnum.IL2CPP_TYPE_I8, ImportReference(typeof(Int64)));
			BuiltInTypes.Add(Il2CppTypeEnum.IL2CPP_TYPE_U8, ImportReference(typeof(UInt64)));
			BuiltInTypes.Add(Il2CppTypeEnum.IL2CPP_TYPE_R4, ImportReference(typeof(Single)));
			BuiltInTypes.Add(Il2CppTypeEnum.IL2CPP_TYPE_R8, ImportReference(typeof(Double)));
			BuiltInTypes.Add(Il2CppTypeEnum.IL2CPP_TYPE_STRING, ImportReference(typeof(String)));
			BuiltInTypes.Add(Il2CppTypeEnum.IL2CPP_TYPE_TYPEDBYREF, ImportReference(typeof(TypedReference)));
		}
	}
}
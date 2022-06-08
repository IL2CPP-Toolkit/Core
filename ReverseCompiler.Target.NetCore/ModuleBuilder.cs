using System;
using System.Collections.Generic;
using System.Diagnostics;
using Il2CppToolkit.Model;
using Il2CppToolkit.Runtime;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Il2CppToolkit.ReverseCompiler.Target.NetCore
{
	public class ModuleBuilder
	{
		const MethodAttributes kRTObjGetterAttrs = MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.Virtual | MethodAttributes.RTSpecialName | MethodAttributes.SpecialName | MethodAttributes.NewSlot;
		const MethodAttributes kGetterAttrs = MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.Virtual | MethodAttributes.RTSpecialName | MethodAttributes.SpecialName;
		const MethodAttributes kCtorAttrs = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName;
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

		private readonly TypeReference RuntimeObjectTypeRef;
		private readonly TypeReference IRuntimeObjectTypeRef;
		private readonly TypeReference IMemorySourceTypeRef;
		private readonly MethodReference ObjectCtorMethodRef;
		private readonly MethodReference GetMemberValueMethodRef;
		private ModuleDefinition Module => AssemblyDefinition.MainModule;

		public ModuleBuilder(ICompileContext context, AssemblyDefinition assemblyDefinition)
		{
			Context = context;
			AssemblyDefinition = assemblyDefinition;
			AddBuiltInTypes(Module);
			RuntimeObjectTypeRef = Module.ImportReference(typeof(RuntimeObject));
			IRuntimeObjectTypeRef = Module.ImportReference(typeof(IRuntimeObject));
			IMemorySourceTypeRef = Module.ImportReference(typeof(IMemorySource));
			ObjectCtorMethodRef = Module.ImportReference(typeof(object).GetConstructor(Type.EmptyTypes));
			GetMemberValueMethodRef = Module.ImportReference(typeof(FieldMember<,>).GetMethod("GetValue"));
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
				Module.Types.Add(typeDef);

			EnqueuedTypes.Add(cppTypeDef);
			TypeDefinitionQueue.Enqueue(cppTypeDef);
			return typeDef;
		}

		private void BuildDefinitionQueue()
		{
			while (TypeDefinitionQueue.TryDequeue(out Il2CppTypeDefinition cppTypeDef))
			{
				TypeDefinition typeDef = GetTypeDefinition(cppTypeDef);
				InitializeTypeDefinition(cppTypeDef, typeDef);
				DefineConstructors(typeDef);

				using TypeInfoBuilder typeInfo = new(typeDef, Module);
				DefineFields(cppTypeDef, typeDef, typeInfo);
			}
		}

		private void InitializeTypeDefinition(Il2CppTypeDefinition cppTypeDef, TypeDefinition typeDef)
		{
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

		private void DefineConstructors(TypeDefinition typeDef)
		{
			if (typeDef.IsInterface)
				return;

			if (typeDef.IsValueType)
			{
				// implement IRuntimeObject
				ImplementIRuntimeObject(typeDef);
				return;
			}

			if (typeDef.BaseType == null || typeDef.BaseType == Module.TypeSystem.Object)
			{
				// inherit from RuntimeObject
				typeDef.BaseType = RuntimeObjectTypeRef;
			}
			MethodDefinition ctorMethod = new(".ctor", kCtorAttrs, Module.TypeSystem.Void);
			ctorMethod.Parameters.Add(new ParameterDefinition(IMemorySourceTypeRef));
			ctorMethod.Parameters.Add(new ParameterDefinition(Module.TypeSystem.UInt64));

			MethodReference baseCtor = typeDef.BaseType.GetConstructor();
			foreach (ParameterDefinition paramDef in ctorMethod.Parameters)
				baseCtor.Parameters.Add(paramDef);

			ILProcessor ctorMethodIL = ctorMethod.Body.GetILProcessor();
			ctorMethodIL.Emit(OpCodes.Ldarg_0);         // this
			ctorMethodIL.Emit(OpCodes.Ldarg_1);         // memorySource
			ctorMethodIL.Emit(OpCodes.Ldarg_2);         // address
			ctorMethodIL.Emit(OpCodes.Call, baseCtor);  // instance void base::.ctor(class [Il2CppToolkit.Runtime]Il2CppToolkit.Runtime.IMemorySource, unsigned int64)
			ctorMethodIL.Emit(OpCodes.Ret);
			typeDef.Methods.Add(ctorMethod);
		}

		private void ImplementIRuntimeObject(TypeDefinition typeDef)
		{
			typeDef.Interfaces.Add(new InterfaceImplementation(IRuntimeObjectTypeRef));
			FieldDefinition AddIRuntimeObjectProperty(string name, TypeReference typeReference)
			{
				FieldDefinition fieldDef = new($"<IRuntimeObject.{name}>k__BackingField", FieldAttributes.Private | FieldAttributes.InitOnly, typeReference);
				MethodDefinition getMethodDef = new($"IRuntimeObject.get_{name}", kRTObjGetterAttrs, typeReference);
				ILProcessor getMethodIL = getMethodDef.Body.GetILProcessor();
				getMethodIL.Emit(OpCodes.Ldarg_0);
				getMethodIL.Emit(OpCodes.Ldfld, fieldDef);
				getMethodIL.Emit(OpCodes.Ret);
				PropertyDefinition propertyDef = new($"IRuntimeObject.{name}", PropertyAttributes.None, typeReference);
				typeDef.Fields.Add(fieldDef);
				typeDef.Methods.Add(getMethodDef);
				typeDef.Properties.Add(propertyDef);
				return fieldDef;
			}
			FieldDefinition sourceFieldDef = AddIRuntimeObjectProperty("Source", IMemorySourceTypeRef);
			FieldDefinition addressFieldDef = AddIRuntimeObjectProperty("Address", Module.TypeSystem.UInt64);
			MethodDefinition ctorMethod = new(".ctor", kCtorAttrs, Module.TypeSystem.Void);
			ctorMethod.Parameters.Add(new ParameterDefinition(IMemorySourceTypeRef));
			ctorMethod.Parameters.Add(new ParameterDefinition(Module.TypeSystem.UInt64));
			ILProcessor ctorMethodIL = ctorMethod.Body.GetILProcessor();
			// valuetype doesn't need to call base ctor
			if (!typeDef.IsValueType)
			{
				ctorMethodIL.Emit(OpCodes.Ldarg_0);
				ctorMethodIL.Emit(OpCodes.Call, ObjectCtorMethodRef);
			}

			ctorMethodIL.Emit(OpCodes.Ldarg_0);                 // this
			ctorMethodIL.Emit(OpCodes.Ldarg_1);                 // memorySource
			ctorMethodIL.Emit(OpCodes.Stfld, sourceFieldDef);   // class [Il2CppToolkit.Runtime]Il2CppToolkit.Runtime.IMemorySource T::__source

			ctorMethodIL.Emit(OpCodes.Ldarg_0);                 // this
			ctorMethodIL.Emit(OpCodes.Ldarg_2);                 // address
			ctorMethodIL.Emit(OpCodes.Stfld, addressFieldDef);  // unsigned int64 T::__address

			ctorMethodIL.Emit(OpCodes.Ret);
			typeDef.Methods.Add(ctorMethod);
		}

		private void DefineFields(Il2CppTypeDefinition cppTypeDef, TypeDefinition typeDef, TypeInfoBuilder typeInfo)
		{
			var fieldEnd = cppTypeDef.fieldStart + cppTypeDef.field_count;
			for (var i = cppTypeDef.fieldStart; i < fieldEnd; ++i)
			{
				Il2CppFieldDefinition cppFieldDef = Metadata.fieldDefs[i];
				Il2CppType cppFieldType = Il2Cpp.Types[cppFieldDef.typeIndex];
				string fieldName = Metadata.GetStringFromIndex(cppFieldDef.nameIndex);
				TypeReference fieldTypeRef = UseTypeReference(typeDef, cppFieldType);
				FieldAttributes fieldAttrs = (FieldAttributes)cppFieldType.attrs;
				bool isStatic = fieldAttrs.HasFlag(FieldAttributes.Static);
				if (fieldAttrs.HasFlag(FieldAttributes.Literal))
				{
					// TODO add field literal directly
					continue;
				}

				var fieldOffset = Context.Model.GetFieldOffsetFromIndex(cppTypeDef, i);
				if (isStatic)
				{
					if (Context.Model.TypeDefToAddress.TryGetValue(cppTypeDef, out ulong address))
					{
						typeInfo.DefineStaticField(fieldName, fieldTypeRef, Context.Model.ModuleName, address, fieldOffset, 1);
					}
				}
				else
				{
					typeInfo.DefineField(fieldName, fieldTypeRef, fieldOffset, 1);
				}
			}
			// Il2CppTypeDefinition cppParentTypeDef = null;
			// Il2CppType cppParentType = Il2Cpp.Types[cppTypeDef.parentIndex];
			// if (cppParentType.type == Il2CppTypeEnum.IL2CPP_TYPE_GENERICINST)
			// {
			// 	Il2CppGenericClass cppGenericClass = Context.Model.Il2Cpp.MapVATR<Il2CppGenericClass>(cppParentType.data.generic_class);
			// 	cppParentTypeDef = Context.Model.GetGenericClassTypeDefinition(cppGenericClass);
			// }
			// else if (cppParentType.type == Il2CppTypeEnum.IL2CPP_TYPE_CLASS)
			// {
			// 	cppParentTypeDef = Context.Model.GetTypeDefinitionFromIl2CppType(cppParentType);
			// }
			// // parent class missing typeInfo? try to include static fields from there.
			// if (cppParentTypeDef != null && !Context.Model.TypeDefToAddress.TryGetValue(cppParentTypeDef, out _))
			// {

			// }
		}

		// private MethodDefinition GetMethodDefinition(Il2CppMethodDefinition cppMethodDef)
		// {
		// 	if (Methods.TryGetValue(cppMethodDef, out MethodDefinition methodDef))
		// 		return methodDef;

		// 	string methodName = Metadata.GetStringFromIndex(cppMethodDef.nameIndex);

		// 	// TODO: Give it a real return type!
		// 	methodDef = new MethodDefinition(methodName, (MethodAttributes)cppMethodDef.flags, AssemblyDefinition.MainModule.TypeSystem.Void);
		// 	Methods.Add(cppMethodDef, methodDef);
		// 	return methodDef;
		// }

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

			switch (cppType.type)
			{
				case Il2CppTypeEnum.IL2CPP_TYPE_CLASS:
				case Il2CppTypeEnum.IL2CPP_TYPE_VALUETYPE:
					{
						Il2CppTypeDefinition cppTypeDef = Context.Model.GetTypeDefinitionFromIl2CppType(cppType);
						TypeDefinition typeDef = UseTypeDefinition(cppTypeDef);
						return typeDef;
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
						GenericInstanceType genericInstanceType = new(typeDef);
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
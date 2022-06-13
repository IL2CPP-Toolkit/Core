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
	public sealed class TypeInfoBuilder : IDisposable
	{
		const MethodAttributes kGetterAttrs = MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.RTSpecialName | MethodAttributes.SpecialName;
		private static readonly Type FieldMemberType = typeof(FieldMember<,>);
		private static readonly System.Reflection.ConstructorInfo FieldMemberTypeCtor = typeof(FieldMember<,>).GetConstructors()[0];
		private static readonly Type StaticFieldMemberType = typeof(StaticFieldMember<,>);
		private static readonly System.Reflection.ConstructorInfo StaticFieldMemberTypeCtor = typeof(StaticFieldMember<,>).GetConstructors()[0];
		private readonly TypeDefinition DeclaringType;
		private readonly TypeReference DeclaringTypeRef;
		private readonly TypeDefinition TypeInfo;
		private readonly MethodDefinition TypeInfoCctor;
		private readonly ModuleDefinition ModuleDefinition;
		private readonly ILProcessor CctorIL;
		private bool HasAddedTypeInfo = false;

		public TypeInfoBuilder(TypeDefinition declaringType, ModuleDefinition moduleDefinition)
		{
			ModuleDefinition = moduleDefinition;
			DeclaringType = declaringType;
			DeclaringTypeRef = declaringType;
			TypeInfo = new(string.Empty, $"{declaringType.GetSafeName()}_TypeInfo", TypeAttributes.Sealed | TypeAttributes.Abstract | TypeAttributes.NestedPublic | TypeAttributes.BeforeFieldInit, ModuleDefinition.TypeSystem.Object);
			if (DeclaringType.HasGenericParameters)
			{
				foreach (var arg in DeclaringType.GenericParameters)
				{
					GenericParameter p = new(arg.Name, TypeInfo);
					foreach (var constraint in arg.Constraints)
						p.Constraints.Add(constraint);
					p.HasDefaultConstructorConstraint = arg.HasDefaultConstructorConstraint;
					TypeInfo.GenericParameters.Add(p);
				}

				DeclaringTypeRef = declaringType.MakeGenericType(DeclaringType.GenericParameters.ToArray());
				//GenericInstanceType ginst = new(DeclaringTypeRef);
				//foreach (var arg in DeclaringType.GenericParameters)
				//{
				//	ginst.GenericArguments.Add(new GenericParameter(arg.Name, ginst));
				//}
				//DeclaringTypeRef = ginst;
			}
			// StaticClass: TypeAttributes.Sealed | TypeAttributes.Abstract
			// .method private hidebysig static specialname rtspecialname void
			TypeInfoCctor = new(".cctor", 
				MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.Static | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName, 
				moduleDefinition.TypeSystem.Void);
			CctorIL = TypeInfoCctor.Body.GetILProcessor();
			TypeInfo.Methods.Add(TypeInfoCctor);
		}

		public void DefineField(string name, TypeReference fieldType, byte indirection)
		{
			DoWrite();
			GenericInstanceType fieldTypeDefType = ModuleDefinition.ImportReference(FieldMemberType).MakeGenericType(DeclaringTypeRef, fieldType);
			MethodReference ctor = fieldTypeDefType.GetConstructor();
			foreach (var param in FieldMemberTypeCtor.GetParameters())
				ctor.Parameters.Add(new ParameterDefinition(param.Name, (ParameterAttributes)param.Attributes, ModuleDefinition.ImportReference(param.ParameterType)));
			FieldDefinition typeInfoFieldDef = new(name, FieldAttributes.Static | FieldAttributes.Public | FieldAttributes.InitOnly, fieldTypeDefType);
			CctorIL.Emit(OpCodes.Ldstr, name);
			CctorIL.Emit(OpCodes.Ldc_I4_S, (sbyte)indirection);
			CctorIL.Emit(OpCodes.Newobj, ctor);
			CctorIL.Emit(OpCodes.Stsfld, typeInfoFieldDef);
			TypeInfo.Fields.Add(typeInfoFieldDef);

			MethodDefinition instanceGetMethod = new($"get_{name}", kGetterAttrs, fieldType);
			ILProcessor getMethodIL = instanceGetMethod.Body.GetILProcessor();
			MethodReference getValueMethod = new("GetValue", fieldType, fieldTypeDefType) { HasThis = true };
			getValueMethod.Parameters.Add(new ParameterDefinition("obj", ParameterAttributes.None, ModuleDefinition.ImportReference(typeof(IRuntimeObject))));
			GenericInstanceType typeInfoInst = TypeInfo.MakeGenericType(TypeInfo.GenericParameters.ToArray());
			getMethodIL.Emit(OpCodes.Ldsfld, new FieldReference(name, fieldTypeDefType, typeInfoInst));
			getMethodIL.Emit(OpCodes.Ldarg_0);
			getMethodIL.Emit(OpCodes.Callvirt, getValueMethod);
			getMethodIL.Emit(OpCodes.Ret);
			DeclaringType.Methods.Add(instanceGetMethod);
			PropertyDefinition instanceProperty = new(name, PropertyAttributes.None, fieldType)
			{
				GetMethod = instanceGetMethod
			};
			DeclaringType.Properties.Add(instanceProperty);
		}

		public FieldDefinition DefineStaticField(string name, TypeReference fieldType, byte indirection)
		{
			DoWrite();
			TypeReference fieldTypeDefType = ModuleDefinition.ImportReference(StaticFieldMemberType).MakeGenericType(DeclaringTypeRef, fieldType);
			MethodReference ctor = fieldTypeDefType.GetConstructor();

			foreach (var param in StaticFieldMemberTypeCtor.GetParameters())
				ctor.Parameters.Add(new ParameterDefinition(param.Name, (ParameterAttributes)param.Attributes, ModuleDefinition.ImportReference(param.ParameterType)));

			FieldDefinition fieldDef = new(name, FieldAttributes.Static | FieldAttributes.Public | FieldAttributes.InitOnly, fieldTypeDefType);

			CctorIL.Emit(OpCodes.Ldstr, name);
			CctorIL.Emit(OpCodes.Ldc_I4_S, (sbyte)indirection);
			CctorIL.Emit(OpCodes.Newobj, ctor);
			GenericInstanceType typeInfoInst = TypeInfo.MakeGenericType(TypeInfo.GenericParameters.ToArray());
			CctorIL.Emit(OpCodes.Stsfld, new FieldReference(name, fieldTypeDefType, typeInfoInst));
			TypeInfo.Fields.Add(fieldDef);
			return fieldDef;
		}

		private void DoWrite()
		{
			if (HasAddedTypeInfo)
				return;
			DeclaringType.NestedTypes.Add(TypeInfo);
			HasAddedTypeInfo = true;
		}

		public void Dispose()
		{
			if (!HasAddedTypeInfo)
				return;

			CctorIL.Emit(OpCodes.Ret);
		}
	}
}
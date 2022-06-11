using System;
using System.Collections.Generic;
using System.Diagnostics;
using Il2CppToolkit.Model;
using Il2CppToolkit.Runtime;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Il2CppToolkit.ReverseCompiler.Target.NetCore
{
	public class TypeInfoBuilder : IDisposable
	{
		const MethodAttributes kGetterAttrs = MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.Virtual | MethodAttributes.RTSpecialName | MethodAttributes.SpecialName;
		private static readonly Type FieldMemberType = typeof(FieldMember<,>);
		private static readonly System.Reflection.ConstructorInfo FieldMemberTypeCtor = typeof(FieldMember<,>).GetConstructors()[0];
		private static readonly Type StaticFieldMemberType = typeof(StaticFieldMember<,>);
		private static readonly System.Reflection.ConstructorInfo StaticFieldMemberTypeCtor = typeof(StaticFieldMember<,>).GetConstructors()[0];
		private readonly TypeDefinition DeclaringType;
		private readonly TypeDefinition TypeInfo;
		private readonly MethodDefinition TypeInfoCctor;
		private readonly ModuleDefinition ModuleDefinition;
		private readonly ILProcessor CctorIL;
		private bool ShouldWrite = false;

		public TypeInfoBuilder(TypeDefinition declaringType, ModuleDefinition moduleDefinition)
		{
			ModuleDefinition = moduleDefinition;
			DeclaringType = declaringType;
			// StaticClass: TypeAttributes.Sealed | TypeAttributes.Abstract
			TypeInfo = new(string.Empty, $"{declaringType.Name}_TypeInfo", TypeAttributes.Sealed | TypeAttributes.Abstract | TypeAttributes.NestedPublic);
			TypeInfoCctor = new(".cctor", MethodAttributes.Static | MethodAttributes.Public, moduleDefinition.TypeSystem.Void);
			CctorIL = TypeInfoCctor.Body.GetILProcessor();
			TypeInfo.Methods.Add(TypeInfoCctor);
		}

		public void DefineField(string name, TypeReference fieldType, ulong offset, byte indirection)
		{
			try
			{
				fieldType = ModuleDefinition.ImportReference(fieldType);
			}
			catch { }
			ShouldWrite = true;
			TypeReference fieldTypeDefType = ModuleDefinition.ImportReference(FieldMemberType).MakeGenericType(DeclaringType, fieldType);
			MethodReference ctor = new(".ctor", ModuleDefinition.TypeSystem.Void, fieldTypeDefType);
			foreach (var param in FieldMemberTypeCtor.GetParameters())
				ctor.Parameters.Add(new ParameterDefinition(param.Name, (ParameterAttributes)param.Attributes, ModuleDefinition.ImportReference(param.ParameterType)));
			FieldDefinition typeInfoFieldDef = new(name, FieldAttributes.Static | FieldAttributes.Public | FieldAttributes.InitOnly, fieldTypeDefType);
			CctorIL.Emit(OpCodes.Ldstr, name);
			CctorIL.Emit(OpCodes.Ldc_I4_S, (sbyte)indirection);
			CctorIL.Emit(OpCodes.Newobj, ctor);
			CctorIL.Emit(OpCodes.Stsfld, typeInfoFieldDef);
			TypeInfo.Fields.Add(typeInfoFieldDef);

			//MethodDefinition instanceGetMethod = new($"get_{name}", kGetterAttrs, fieldType);
			//ILProcessor getMethodIL = instanceGetMethod.Body.GetILProcessor();
			//MethodReference getValueMethod = new("GetValue", fieldType, fieldTypeDefType);
			//getValueMethod.Parameters.Add(new ParameterDefinition("obj", ParameterAttributes.None, DeclaringType));
			//getMethodIL.Emit(OpCodes.Ldsfld, typeInfoFieldDef);
			//getMethodIL.Emit(OpCodes.Ldarg_0);
			//getMethodIL.Emit(OpCodes.Callvirt, getValueMethod);
			//getMethodIL.Emit(OpCodes.Ret);
			//DeclaringType.Methods.Add(instanceGetMethod);
			//PropertyDefinition instanceProperty = new(name, PropertyAttributes.None, fieldType)
			//{
			//	GetMethod = instanceGetMethod
			//};
			//DeclaringType.Properties.Add(instanceProperty);
		}

		public FieldDefinition DefineStaticField(string name, TypeReference fieldType, string moduleName, ulong address, ulong offset, byte indirection)
		{
			ShouldWrite = true;
			TypeReference fieldTypeDefType = ModuleDefinition.ImportReference(StaticFieldMemberType).MakeGenericType(DeclaringType, fieldType);
			MethodReference ctor = new(".ctor", ModuleDefinition.TypeSystem.Void, fieldTypeDefType);
			foreach (var param in StaticFieldMemberTypeCtor.GetParameters())
				ctor.Parameters.Add(new ParameterDefinition(param.Name, (ParameterAttributes)param.Attributes, ModuleDefinition.ImportReference(param.ParameterType)));
			FieldDefinition fieldDef = new(name, FieldAttributes.Static | FieldAttributes.Public | FieldAttributes.InitOnly, fieldTypeDefType);
			CctorIL.Emit(OpCodes.Ldstr, name);
			CctorIL.Emit(OpCodes.Ldc_I4_S, (sbyte)indirection);
			CctorIL.Emit(OpCodes.Newobj, ctor);
			CctorIL.Emit(OpCodes.Stsfld, fieldDef);
			TypeInfo.Fields.Add(fieldDef);
			return fieldDef;
		}

		public void Dispose()
		{
			if (!ShouldWrite)
				return;

			CctorIL.Emit(OpCodes.Ret);
			DeclaringType.NestedTypes.Add(TypeInfo);
		}
	}
}
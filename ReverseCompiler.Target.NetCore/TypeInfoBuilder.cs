using System;
using System.Linq;
using System.Text.RegularExpressions;
using Il2CppToolkit.Model;
using Il2CppToolkit.Runtime;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Il2CppToolkit.ReverseCompiler.Target.NetCore
{
	public sealed class TypeInfoBuilder : IDisposable
	{
		private static readonly Regex BackingFieldRegex = new("<(.+)>k__BackingField", RegexOptions.Compiled);
		const MethodAttributes kGetterAttrs = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.ReuseSlot | MethodAttributes.SpecialName;
		private static readonly Type StaticFieldMemberType = typeof(StaticFieldMember<,>);
		private static readonly System.Reflection.ConstructorInfo StaticFieldMemberTypeCtor = typeof(StaticFieldMember<,>).GetConstructors()[0];
		private readonly TypeDefinition ForType;
		private readonly TypeReference ForTypeRef;
		private readonly ModuleDefinition ModuleDefinition;
		private readonly ModuleBuilder ModuleBuilder;
		private ILProcessor CctorIL;

		public TypeInfoBuilder(TypeDefinition forType, ModuleDefinition moduleDefinition, ModuleBuilder moduleBuilder)
		{
			ForType = forType;
			ForTypeRef = ForType.MakeGenericType(ForType.GenericParameters.ToArray());
			ModuleDefinition = moduleDefinition;
			ModuleBuilder = moduleBuilder;
		}

		public ILProcessor GetCCtor()
		{
			if (CctorIL != null)
				return CctorIL;

			MethodDefinition cctor = new(
				".cctor",
				MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.Static | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
				ModuleDefinition.TypeSystem.Void);
			CctorIL = cctor.Body.GetILProcessor();
			ForType.Methods.Add(cctor);
			return CctorIL;
		}

		public void DefineMethod(string name, Il2CppType cppReturnType, Il2CppType[] parameters, MethodAttributes methodAttributes)
		{
			if (methodAttributes.HasFlag(MethodAttributes.SpecialName))
				return;


			MethodDefinition methodDef = new(name, methodAttributes, ModuleDefinition.TypeSystem.Void);

			methodDef.ReturnType = ModuleBuilder.UseTypeReference(methodDef, cppReturnType);

			if (methodDef.ReturnType == null)
				return; // TODO: Add warning log

			TypeReference[] paramTypeRefs = parameters.Select(cppParamType => ModuleBuilder.UseTypeReference(methodDef, cppParamType)).ToArray();
			if (paramTypeRefs.Contains(null))
				return; // TODO: Add warning log

			foreach (TypeReference paramTypeRef in paramTypeRefs)
				methodDef.Parameters.Add(new ParameterDefinition(paramTypeRef));

			if (!methodAttributes.HasFlag(MethodAttributes.Abstract))
			{
				ILProcessor methodIL = methodDef.Body.GetILProcessor();
				methodIL.Emit(OpCodes.Newobj, ModuleBuilder.ImportReference(typeof(NotImplementedException)).GetConstructor());
				methodIL.Emit(OpCodes.Throw);
			}
			ForType.Methods.Add(methodDef);
		}

		public void DefineField(string name, TypeReference fieldType, byte indirection)
		{
			GenericInstanceType typeLookupInst = ModuleDefinition.ImportReference(typeof(Il2CppTypeInfoLookup<>)).MakeGenericType(ForTypeRef);
			MethodDefinition instanceGetMethod = new($"get_{name}", kGetterAttrs, fieldType) { HasThis = true, SemanticsAttributes = MethodSemanticsAttributes.Getter };
			ILProcessor getMethodIL = instanceGetMethod.Body.GetILProcessor();

			MethodReference getValueMethod = new("GetValue", fieldType, typeLookupInst) { HasThis = false };
			getValueMethod.GenericParameters.Add(new GenericParameter("TValue", getValueMethod));
			GenericInstanceMethod getValueMethodInst = getValueMethod.MakeGeneric(fieldType);
			getValueMethodInst.ReturnType = getValueMethod.GenericParameters[0];
			getValueMethodInst.Parameters.Add(new ParameterDefinition("instance", ParameterAttributes.None, ModuleDefinition.ImportReference(typeof(IRuntimeObject))));
			getValueMethodInst.Parameters.Add(new ParameterDefinition("name", ParameterAttributes.None, ModuleDefinition.TypeSystem.String));
			getValueMethodInst.Parameters.Add(new ParameterDefinition("indirection", ParameterAttributes.HasDefault, ModuleDefinition.TypeSystem.Byte));

			getMethodIL.Emit(OpCodes.Ldarg_0);
			getMethodIL.Emit(OpCodes.Ldstr, name);
			getMethodIL.EmitByte(indirection);
			getMethodIL.Emit(OpCodes.Call, getValueMethodInst);
			getMethodIL.Emit(OpCodes.Ret);
			ForType.Methods.Add(instanceGetMethod);
			PropertyDefinition instanceProperty = new(name, PropertyAttributes.None, fieldType)
			{
				HasThis = true,
				GetMethod = instanceGetMethod
			};
			ForType.Properties.Add(instanceProperty);
		}

		public FieldDefinition DefineStaticField(string name, TypeReference fieldType, byte indirection)
		{
			TypeReference fieldTypeDefType = ModuleDefinition.ImportReference(StaticFieldMemberType).MakeGenericType(ForTypeRef, fieldType);
			MethodReference ctor = fieldTypeDefType.GetConstructor();

			foreach (var param in StaticFieldMemberTypeCtor.GetParameters())
				ctor.Parameters.Add(new ParameterDefinition(param.Name, (ParameterAttributes)param.Attributes, ModuleDefinition.ImportReference(param.ParameterType)));

			FieldDefinition fieldDef = new(name, FieldAttributes.Static | FieldAttributes.Public | FieldAttributes.InitOnly, fieldTypeDefType);
			ForType.Fields.Add(fieldDef);

			FieldReference fieldRef = new(name, fieldTypeDefType, ForTypeRef);

			ILProcessor cctorIl = GetCCtor();
			cctorIl.Emit(OpCodes.Ldstr, name);
			cctorIl.EmitByte(indirection);
			cctorIl.Emit(OpCodes.Newobj, ctor);
			cctorIl.Emit(OpCodes.Stsfld, fieldRef);

			return fieldDef;
		}

		public void Dispose()
		{
			CctorIL?.Emit(OpCodes.Ret);
		}
	}
}
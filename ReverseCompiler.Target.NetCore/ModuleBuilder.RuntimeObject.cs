using System;
using System.Runtime.CompilerServices;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Il2CppToolkit.ReverseCompiler.Target.NetCore
{
	public partial class ModuleBuilder
	{
		const MethodAttributes kRTObjGetterAttrs = MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.Virtual | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.SpecialName;
		const MethodAttributes kCtorAttrs = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName;

		private void CreateDefaultConstructor(TypeDefinition typeDef)
		{
			if (typeDef.IsValueType)
				return;

			MethodDefinition ctorMethod = new(".ctor", kCtorAttrs, ImportReference(typeof(void)));
			ILProcessor ctorMethodIL = ctorMethod.Body.GetILProcessor();
			ctorMethodIL.Emit(OpCodes.Ldarg_0);                       // this
			if (typeDef.BaseType != null)
			{
				MethodReference baseCtor = typeDef.BaseType.GetConstructor(Module);
				ctorMethodIL.Emit(OpCodes.Call, baseCtor);            // instance void base::.ctor()
			}
			else if (!typeDef.IsValueType)
			{
				ctorMethodIL.Emit(OpCodes.Call, ObjectCtorMethodRef); // instance void System.Object::.ctor()
			}
			ctorMethodIL.Emit(OpCodes.Ret);
			typeDef.Methods.Add(ctorMethod);
		}

		private void DefineConstructors(TypeDefinition typeDef)
		{
			if (typeDef.IsInterface)
				return;

			if (typeDef.BaseType == null || typeDef.BaseType == ImportReference(typeof(object)))
			{
				// inherit from RuntimeObject
				typeDef.BaseType = RuntimeObjectTypeRef;
			}
			CreateDefaultConstructor(typeDef);

			if (typeDef.IsValueType)
			{
				// implement IRuntimeObject
				ImplementIRuntimeObject(typeDef);
				return;
			}
			MethodDefinition ctorMethod = new(".ctor", kCtorAttrs, ImportReference(typeof(void)));
			ctorMethod.Parameters.Add(new ParameterDefinition(IMemorySourceTypeRef));
			ctorMethod.Parameters.Add(new ParameterDefinition(ImportReference(typeof(UInt64))));

			MethodReference baseCtor = typeDef.BaseType.GetConstructor(Module);
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
			TypeReference thisTypeRef = typeDef.AsGenericThis();
			typeDef.Interfaces.Add(new InterfaceImplementation(IRuntimeObjectTypeRef));
			FieldReference AddIRuntimeObjectProperty(string name, TypeReference typeReference)
			{
				FieldDefinition fieldDef = new($"<{name}>k__BackingField", FieldAttributes.Private | FieldAttributes.InitOnly, typeReference);
				fieldDef.CustomAttributes.Add(new CustomAttribute(ImportReference(typeof(CompilerGeneratedAttribute)).GetConstructor(Module)));
				typeDef.Fields.Add(fieldDef);
				FieldReference fieldRef = new(fieldDef.Name, fieldDef.FieldType, thisTypeRef);

				MethodDefinition getMethodDef = new($"get_{name}", kRTObjGetterAttrs, typeReference)
				{
					SemanticsAttributes = MethodSemanticsAttributes.Getter
				};
				getMethodDef.CustomAttributes.Add(new CustomAttribute(ImportReference(typeof(CompilerGeneratedAttribute)).GetConstructor(Module)));
				typeDef.Methods.Add(getMethodDef);
				ILProcessor getMethodIL = getMethodDef.Body.GetILProcessor();
				getMethodIL.Emit(OpCodes.Ldarg_0);
				getMethodIL.Emit(OpCodes.Ldfld, fieldRef);
				getMethodIL.Emit(OpCodes.Ret);

				PropertyDefinition propertyDef = new($"{name}", PropertyAttributes.None, typeReference)
				{
					GetMethod = getMethodDef
				};
				typeDef.Properties.Add(propertyDef);
				return fieldRef;
			}
			FieldReference sourceFieldRef = AddIRuntimeObjectProperty("Source", IMemorySourceTypeRef);
			FieldReference addressFieldRef = AddIRuntimeObjectProperty("Address", ImportReference(typeof(UInt64)));

			MethodDefinition defaultCtorMethod = new(".ctor", kCtorAttrs, ImportReference(typeof(void)));
			defaultCtorMethod.Body.GetILProcessor().Emit(OpCodes.Ldarg_0);
			defaultCtorMethod.Body.GetILProcessor().Emit(OpCodes.Ldc_I4_0);
			defaultCtorMethod.Body.GetILProcessor().Emit(OpCodes.Conv_I8);
			defaultCtorMethod.Body.GetILProcessor().Emit(OpCodes.Stfld, addressFieldRef);
			defaultCtorMethod.Body.GetILProcessor().Emit(OpCodes.Ldarg_0);
			defaultCtorMethod.Body.GetILProcessor().Emit(OpCodes.Ldnull);
			defaultCtorMethod.Body.GetILProcessor().Emit(OpCodes.Stfld, sourceFieldRef);
			defaultCtorMethod.Body.GetILProcessor().Emit(OpCodes.Ret);
			typeDef.Methods.Add(defaultCtorMethod);

			MethodDefinition ctorMethod = new(".ctor", kCtorAttrs, ImportReference(typeof(void)));
			ctorMethod.Parameters.Add(new ParameterDefinition(IMemorySourceTypeRef));
			ctorMethod.Parameters.Add(new ParameterDefinition(ImportReference(typeof(UInt64))));
			ILProcessor ctorMethodIL = ctorMethod.Body.GetILProcessor();
			// valuetype doesn't need to call base ctor
			if (!typeDef.IsValueType)
			{
				ctorMethodIL.Emit(OpCodes.Ldarg_0);
				ctorMethodIL.Emit(OpCodes.Call, ObjectCtorMethodRef);
			}

			ctorMethodIL.Emit(OpCodes.Ldarg_0);                 // this
			ctorMethodIL.Emit(OpCodes.Ldarg_1);                 // memorySource
			ctorMethodIL.Emit(OpCodes.Stfld, sourceFieldRef);   // class [Il2CppToolkit.Runtime]Il2CppToolkit.Runtime.IMemorySource T::__source

			ctorMethodIL.Emit(OpCodes.Ldarg_0);                 // this
			ctorMethodIL.Emit(OpCodes.Ldarg_2);                 // address
			ctorMethodIL.Emit(OpCodes.Stfld, addressFieldRef);  // unsigned int64 T::__address

			ctorMethodIL.Emit(OpCodes.Ret);
			typeDef.Methods.Add(ctorMethod);
		}
	}
}
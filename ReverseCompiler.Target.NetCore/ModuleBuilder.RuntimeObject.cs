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
	public partial class ModuleBuilder
	{
		private void CreateDefaultConstructor(TypeDefinition typeDef)
		{
			if (typeDef.IsValueType)
				return;

			MethodDefinition ctorMethod = new(".ctor", kCtorAttrs, ImportReference(typeof(void)));
			ILProcessor ctorMethodIL = ctorMethod.Body.GetILProcessor();
			ctorMethodIL.Emit(OpCodes.Ldarg_0);                       // this
			if (typeDef.BaseType != null)
			{
				MethodReference baseCtor = typeDef.BaseType.GetConstructor();
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
			FieldDefinition addressFieldDef = AddIRuntimeObjectProperty("Address", ImportReference(typeof(UInt64)));
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
			ctorMethodIL.Emit(OpCodes.Stfld, sourceFieldDef);   // class [Il2CppToolkit.Runtime]Il2CppToolkit.Runtime.IMemorySource T::__source

			ctorMethodIL.Emit(OpCodes.Ldarg_0);                 // this
			ctorMethodIL.Emit(OpCodes.Ldarg_2);                 // address
			ctorMethodIL.Emit(OpCodes.Stfld, addressFieldDef);  // unsigned int64 T::__address

			ctorMethodIL.Emit(OpCodes.Ret);
			typeDef.Methods.Add(ctorMethod);
		}
	}
}
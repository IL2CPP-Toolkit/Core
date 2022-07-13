using System;
using System.Collections.Generic;
using System.Linq;
using Il2CppToolkit.Common.Errors;
using Il2CppToolkit.Model;
using Il2CppToolkit.Runtime;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Il2CppToolkit.ReverseCompiler.Target.NetCore
{
	public partial class ModuleBuilder
	{
		private void DefineMethods(Il2CppTypeDefinition cppTypeDef, TypeDefinition typeDef)
		{
			int methodEnd = cppTypeDef.methodStart + cppTypeDef.method_count;
			for (var i = cppTypeDef.methodStart; i < methodEnd; ++i)
			{
				Il2CppMethodDefinition cppMethodDef = Metadata.methodDefs[i];
				MethodDefinition methodDef = DefineMethod(typeDef, cppMethodDef);
				if (methodDef == null)
					continue;
				typeDef.Methods.Add(methodDef);
				MethodDefs.Add(i, methodDef);
			}
		}

		public MethodDefinition DefineMethod(TypeDefinition typeDef, Il2CppMethodDefinition cppMethodDef)
		{
			MethodAttributes methodAttributes = (MethodAttributes)cppMethodDef.flags;
			string name = Metadata.GetStringFromIndex(cppMethodDef.nameIndex);

			MethodDefinition existingMethod = typeDef.Methods.FirstOrDefault(method => method.Name == name);
			if (existingMethod != null)
			{
				// already have this method? assume we generated it for a passthrough field
				ErrorHandler.Assert(
					methodAttributes.HasFlag(MethodAttributes.SpecialName) && (name.StartsWith("get_") || name.StartsWith("set_")),
					"Existing method without [SpecialName] found");
				existingMethod.Attributes |= methodAttributes & ~MethodAttributes.MemberAccessMask;
				Context.Logger?.LogInfo($"Skipping existing method: {typeDef.FullName}.{name} ");
				return null;
			}

			if (name.StartsWith("op_") && methodAttributes.HasFlag(MethodAttributes.SpecialName))
			{
				Context.Logger?.LogInfo($"Skipping operator (unsupported): {typeDef.FullName}.{name} ");
				return null;
			}

			bool isStatic = methodAttributes.HasFlag(MethodAttributes.Static);

			Il2CppType cppReturnType = Il2Cpp.Types[cppMethodDef.returnType];
			MethodDefinition methodDef = new(name, methodAttributes, ImportReference(typeof(void)))
			{
				DeclaringType = typeDef,
			};

			if (cppMethodDef.genericContainerIndex >= 0)
			{
				var genericContainer = Metadata.genericContainers[cppMethodDef.genericContainerIndex];
				for (int j = 0; j < genericContainer.type_argc; j++)
				{
					var genericParameterIndex = genericContainer.genericParameterStart + j;
					var param = Metadata.genericParameters[genericParameterIndex];
					var genericParameter = CreateGenericParameter(param, methodDef);
					methodDef.GenericParameters.Add(genericParameter);
				}
			}

			if (isStatic)
			{
				ParameterDefinition runtimeParam = new("runtime", ParameterAttributes.None, IRuntimeObjectTypeRef);
				methodDef.Parameters.Add(runtimeParam);
			}

			methodDef.ReturnType = UseTypeReference(methodDef, cppReturnType);
			if (methodDef.ReturnType == null)
			{
				Context.Logger?.LogWarning($"{typeDef.FullName}.{name}(...) Unsupported return type");
				return null;
			}

			int paramEnd = cppMethodDef.parameterStart + cppMethodDef.parameterCount;
			for (int p = cppMethodDef.parameterStart; p < paramEnd; ++p)
			{
				Il2CppParameterDefinition cppParamDef = Metadata.parameterDefs[p];
				Il2CppType cppParamType = Il2Cpp.Types[cppParamDef.typeIndex];
				string paramName = Metadata.GetStringFromIndex(cppParamDef.nameIndex);
				TypeReference paramTypeRef = UseTypeReference(methodDef, cppParamType);
				if (paramTypeRef == null)
				{
					Context.Logger?.LogWarning($"{typeDef.FullName}.{name}(...) Unsupported parameter type");
					return null;
				}
				methodDef.Parameters.Add(new ParameterDefinition(paramName, ParameterAttributes.None, paramTypeRef));
			}

			// explicit interface impl?
			int lastDot = name.LastIndexOf('.');
			if (methodDef.IsPrivate && methodDef.IsFinal && methodDef.IsVirtual && lastDot >= 0)
			{
				ErrorHandler.Assert(lastDot != -1, "explicit impl without interface in name");
				string interfaceName = name.Substring(0, lastDot);
				string methodName = name.Substring(lastDot + 1);
				Queue<TypeReference> openRefs = new();
				openRefs.Enqueue(typeDef);
				while (openRefs.TryDequeue(out TypeReference typeRef))
				{
					if (typeRef.FullName == interfaceName)
					{
						// if (typeRef is TypeDefinition overrideMethodOwner)
						// {
						// 	methodDef.Overrides.Add(overrideMethodOwner.Methods
						// 		.Single(method => method.Name == methodName && method.Parameters.Count == methodDef.Parameters.Count)
						// 		);
						// }
						// else
						// {
						// 	ErrorHandler.Assert(false, "Cannot override non-generated members");
						// }

						MethodReference overrideMethod;
						methodDef.Overrides.Add(overrideMethod = new MethodReference(methodName, methodDef.ReturnType, typeRef)
						{
							HasThis = methodDef.HasThis,
						});
						overrideMethod.Parameters.AddRange(methodDef.Parameters);
						break;
					}
					if (typeRef is TypeDefinition typeRefDef)
					{
						if (typeRefDef.BaseType != null)
							openRefs.Enqueue(typeRefDef.BaseType);
						foreach (var iface in typeRefDef.Interfaces)
							openRefs.Enqueue(iface.InterfaceType);
					}
				}
			}
			else
			{
				methodDef.Attributes &= ~MethodAttributes.MemberAccessMask;
				methodDef.Attributes |= MethodAttributes.Public;
			}

			TypeReference classTypeRef = typeDef;
			if (typeDef.HasGenericParameters)
				classTypeRef = typeDef.MakeGenericType(typeDef.GenericParameters);
			GenericInstanceType typeLookupInst = Module.ImportReference(typeof(Il2CppTypeInfoLookup<>)).MakeGenericType(classTypeRef);
			MethodReference callMethodInst = PrepareMethodRefAndGetImplementationToCall(methodDef, cppReturnType, typeLookupInst);
			if (!methodAttributes.HasFlag(MethodAttributes.Abstract))
			{
				ILProcessor methodIL = methodDef.Body.GetILProcessor();
				methodIL.Emit(OpCodes.Ldarg_0);
				methodIL.Emit(OpCodes.Ldstr, name);
				methodIL.EmitI4(cppMethodDef.parameterCount);
				methodIL.Emit(OpCodes.Newarr, ImportReference(typeof(object)));
				for (byte p = 0; p < cppMethodDef.parameterCount; ++p)
				{
					methodIL.Emit(OpCodes.Dup);
					methodIL.EmitI4(p);
					methodIL.EmitArg(p + 1);
					TypeReference paramType = methodDef.Parameters[p].ParameterType;
					if (paramType.IsGenericInstance && paramType is GenericInstanceType genericInstance && genericInstance.Name == "Nullable`1")
					{
						var nullableType = ImportReference(typeof(Nullable<>)).MakeGenericType(genericInstance.ElementType.GenericParameters);
						var nullableArgCtor = ImportReference(typeof(NullableArg<>)).MakeGenericType(genericInstance.GenericArguments).GetConstructor(nullableType);
						methodIL.Emit(OpCodes.Newobj, nullableArgCtor);
					}
					else if (paramType.IsValueType)
					{
						methodIL.Emit(OpCodes.Box, paramType);
					}
					methodIL.Emit(OpCodes.Stelem_Ref);
				}
				methodIL.Emit(OpCodes.Call, callMethodInst);
				methodIL.Emit(OpCodes.Ret);
			}
			return methodDef;
		}

		private MethodReference PrepareMethodRefAndGetImplementationToCall(MethodDefinition methodDef, Il2CppType returnType, GenericInstanceType typeLookupInst)
		{
			TypeReference typeArg = methodDef.ReturnType;
			TypeReference returnTypeRef = typeArg;
			if (returnType.type == Il2CppTypeEnum.IL2CPP_TYPE_VOID)
			{
				MethodReference callMethod = new("CallMethod", methodDef.ReturnType, typeLookupInst) { HasThis = false };
				callMethod.Parameters.Add(new ParameterDefinition(IRuntimeObjectTypeRef));
				callMethod.Parameters.Add(new ParameterDefinition(ImportReference(typeof(string))));
				callMethod.Parameters.Add(new ParameterDefinition(new ArrayType(ImportReference(typeof(object)))));
				return callMethod;
			}
			else
			{
				string methodName = "CallMethod";
				MethodReference callMethod = new(methodName, methodDef.ReturnType, typeLookupInst) { HasThis = false };
				callMethod.GenericParameters.Add(new GenericParameter("TValue", callMethod));
				callMethod.Parameters.Add(new ParameterDefinition(IRuntimeObjectTypeRef));
				callMethod.Parameters.Add(new ParameterDefinition(ImportReference(typeof(string))));
				callMethod.Parameters.Add(new ParameterDefinition(new ArrayType(ImportReference(typeof(object)))));
				callMethod.ReturnType = callMethod.GenericParameters[0];
				GenericInstanceMethod callMethodInst = callMethod.MakeGeneric(typeArg);
				return callMethodInst;
			}
		}
	}
}
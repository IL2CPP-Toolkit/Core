using System;
using System.Reflection;
using System.Reflection.Emit;
using Il2CppToolkit.Model;
using Il2CppToolkit.Runtime;
using Il2CppToolkit.Runtime.Types;
using Il2CppToolkit.Runtime.Types.Reflection;

namespace Il2CppToolkit.ReverseCompiler.Target.NetCore
{
    public class GeneratedType
    {
        public Type Type;
        public TypeBuilder StaticType;
        public TypeDescriptor Descriptor;
        private bool IsCreated = false;

        public GeneratedType(Type type, TypeDescriptor td)
        {
            Type = type;
            Descriptor = td;
        }

        public void Create()
        {
            if (IsCreated)
                return;

            if (Type is TypeBuilder tb)
            {
                try
                {
                    tb.CreateType();
                }
                catch (InvalidOperationException)
                {
                    // This is needed to throw away InvalidOperationException.
                    // Loader might send the TypeResolve event more than once
                    // and the type might be complete already.
                }
                catch (Exception ex)
                {
                    CompilerError.ResolveTypeError.Raise($"Failed to resolve type. Exception={ex}");
                }
            }
            StaticType?.CreateType();
            IsCreated = true;
        }

        internal static void CreateConstructor(TypeBuilder tb, Type[] ctorArgs, ConstructorInfo ctorInfo)
        {
            ConstructorBuilder ctor = tb.DefineConstructor(MethodAttributes.Public,
                CallingConventions.Standard | CallingConventions.HasThis, ctorArgs);
            ILGenerator ilCtor = ctor.GetILGenerator();
            ilCtor.Emit(OpCodes.Ldarg_0);
            ilCtor.Emit(OpCodes.Ldarg_1);
            ilCtor.Emit(OpCodes.Ldarg_2);
            ilCtor.Emit(OpCodes.Call, ctorInfo);
            ilCtor.Emit(OpCodes.Ret);
        }

        public void CreateConstructor()
        {
            if (Type is not TypeBuilder tb)
                return;

            // constructor
            if (!Descriptor.TypeDef.IsEnum && !Descriptor.Attributes.HasFlag(TypeAttributes.Interface))
            {
                if (Descriptor.TypeDef.IsValueType)
                {
                    tb.DefineDefaultConstructor(MethodAttributes.Public);
                }
                else
                {
                    if (Descriptor.IsStatic)
                    {
                        ConstructorInfo ctorInfo = TypeBuilder.GetConstructor(typeof(StaticInstance<>).MakeGenericType(tb), StaticReflectionHandles.StaticInstance.Ctor.ConstructorInfo);
                        CreateConstructor(tb, StaticReflectionHandles.StaticInstance.Ctor.Parameters, ctorInfo);
                    }
                    else
                    {
                        CreateConstructor(tb, StaticReflectionHandles.StructBase.Ctor.Parameters, StaticReflectionHandles.StructBase.Ctor.ConstructorInfo);
                    }
                }
            }
        }

        public TypeBuilder EnsureStaticType()
        {
            if (StaticType == null)
            {
                if (!(Type is TypeBuilder tb))
                    throw new ApplicationException("Cannot build nested types for non-generated types");

                if (Descriptor.GenericParameterNames != null && Descriptor.GenericParameterNames.Length > 0)
                    return null;

                if (Descriptor.TypeInfo == null)
                    return null;

                StaticType = tb.DefineNestedType("StaticFields", TypeAttributes.NestedPublic, typeof(StructBase));
                DefineTypesPhase.CreateConstructor(StaticType, StaticReflectionHandles.StructBase.Ctor.Parameters, StaticReflectionHandles.StructBase.Ctor.ConstructorInfo);

                MethodBuilder mb = tb.DefineMethod("GetStaticFields", MethodAttributes.Public | MethodAttributes.NewSlot | MethodAttributes.Static, StaticType, new Type[] { typeof(Il2CsRuntimeContext) });
                ILGenerator mbil = mb.GetILGenerator();
                mbil.DeclareLocal(typeof(ulong));
                mbil.Emit(OpCodes.Ldc_I8, (long)Descriptor.TypeInfo.Address);
                mbil.Emit(OpCodes.Ldarg_0); // context
                mbil.Emit(OpCodes.Ldstr, Descriptor.TypeInfo.ModuleName);
                mbil.EmitCall(OpCodes.Callvirt, Il2CppRuntimeContext_Types.GetModuleAddress, null);
                mbil.Emit(OpCodes.Add);
                mbil.Emit(OpCodes.Stloc_0); // address

                mbil.Emit(OpCodes.Ldarg_0); // context
                mbil.Emit(OpCodes.Ldloc_0); // address
                mbil.Emit(OpCodes.Ldc_I4_1);
                mbil.EmitCall(OpCodes.Call, MemorySourceExtensions_Types.ReadValue.MakeGenericMethod(typeof(ClassDefinition)), null);
                mbil.EmitCall(OpCodes.Callvirt, ClassDefinition_Types.get_StaticFields, null);
                mbil.EmitCall(OpCodes.Callvirt, StructBase_Types.As.MakeGenericMethod(StaticType), null);
                mbil.Emit(OpCodes.Ret);
            }
            return StaticType;
        }

        public static class Il2CppRuntimeContext_Types
        {
            public static readonly MethodInfo GetModuleAddress = typeof(Il2CsRuntimeContext).GetMethod("GetModuleAddress");
        }

        public static class MemorySourceExtensions_Types
        {
            public static readonly MethodInfo ReadValue = typeof(MemorySourceExtensions).GetMethod("ReadValue", new Type[] { typeof(IMemorySource), typeof(ulong), typeof(byte) });
        }

        public static class ClassDefinition_Types
        {
            public static readonly MethodInfo get_StaticFields = typeof(ClassDefinition).GetMethod("get_StaticFields", BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetProperty, null, CallingConventions.HasThis, Array.Empty<Type>(), null);
        }

        public static class StructBase_Types
        {
            public static readonly MethodInfo As = typeof(StructBase).GetMethod("As", Array.Empty<Type>());
        }

    }
}
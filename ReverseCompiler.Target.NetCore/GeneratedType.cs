using System;
using System.Reflection;
using System.Reflection.Emit;
using Il2CppToolkit.Model;
using Il2CppToolkit.Runtime;
using Il2CppToolkit.Runtime.Types;
using Il2CppToolkit.Runtime.Types.Reflection;

namespace Il2CppToolkit.ReverseCompiler.Target.NetCore
{
    public interface IGeneratedType
    {
        Type Type { get; }
        TypeDescriptor Descriptor { get; }
        void Create();
        void Build(BuildTypeResolver resolver);
    }

    public static class GeneratedTypeFactory
    {
        public static IGeneratedType Make(Type type, TypeDescriptor descriptor)
        {
            if (type is TypeBuilder tb)
            {
                return new GeneratedType(tb, descriptor);
            }
            return new BuiltInType(type, descriptor);
        }
    }

    public class BuiltInType : IGeneratedType
    {
        public Type Type { get; }
        public TypeDescriptor Descriptor { get; }
        public void Build(BuildTypeResolver resolver) { }
        public void Create() { }

        public BuiltInType(Type type, TypeDescriptor descriptor)
        {
            Type = type;
            Descriptor = descriptor;
        }
    }

    public class GeneratedType : IGeneratedType
    {
        private static readonly Type[] CtorArgs = new Type[] { typeof(IMemorySource) /*source*/, typeof(ulong) /*address*/ };
        private static readonly ConstructorInfo Object_Ctor = typeof(object).GetConstructor(Type.EmptyTypes);
        private const MethodAttributes InstanceGetterAttribs = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.RTSpecialName | MethodAttributes.SpecialName;

        private TypeBuilder TypeBuilder;
        private bool IsCreated = false;

        public Type Type => TypeBuilder;
        public TypeDescriptor Descriptor { get; }

        public GeneratedType(TypeBuilder tb, TypeDescriptor td)
        {
            TypeBuilder = tb;
            Descriptor = td;
        }

        public void Create()
        {
            if (IsCreated)
                return;

            try
            {
                TypeBuilder.CreateType();
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
            IsCreated = true;
        }

        public void Build(BuildTypeResolver typeResolver)
        {
            if (Descriptor.TypeDef.IsEnum)
            {
                BuildEnum(typeResolver);
                return;
            }

            if (Descriptor.Base != null)
            {
                TypeBuilder.SetParent(typeResolver.ResolveTypeReference(Descriptor.Base));
            }

            CreateConstructor();
        }

        private void BuildEnum(BuildTypeResolver typeResolver)
        {
            foreach (FieldDescriptor field in Descriptor.Fields)
            {
                FieldBuilder fb = TypeBuilder.DefineField(field.Name, typeResolver.ResolveTypeReference(field.Type), field.Attributes);
                if (field.DefaultValue != null)
                    fb.SetConstant(field.DefaultValue);
            }
        }

        private FieldBuilder AddInitField<T>(string name, FieldAttributes fieldAttribs)
        {
            FieldBuilder fb = TypeBuilder.DefineField($"<{name}>k__BackingField", typeof(ulong), FieldAttributes.Private | FieldAttributes.InitOnly);

            MethodBuilder mb = TypeBuilder.DefineMethod($"get_{name}", InstanceGetterAttribs, typeof(long), Type.EmptyTypes);
            ILGenerator mbil = mb.GetILGenerator();
            {
                mbil.Emit(OpCodes.Ldarg_0);
                mbil.Emit(OpCodes.Ldfld, fb);
                mbil.Emit(OpCodes.Ret);
            }

            PropertyBuilder pb = TypeBuilder.DefineProperty(name, PropertyAttributes.None, typeof(T), null);
            pb.SetGetMethod(mb);
            return fb;
        }

        private void CreateConstructor()
        {
            if (TypeBuilder.IsInterface || TypeBuilder.IsEnum || Descriptor.IsStatic)
                return;

            if (TypeBuilder.BaseType is TypeBuilder)
                InitDerivedType();
            else
                InitBaseType();
        }

        private void InitBaseType()
        {
            FieldBuilder fbAddress = AddInitField<ulong>("Address", FieldAttributes.Private | FieldAttributes.InitOnly);
            FieldBuilder fbSource = AddInitField<IMemorySource>("Source", FieldAttributes.Private | FieldAttributes.InitOnly);

            ConstructorBuilder ctor = TypeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard | CallingConventions.HasThis, CtorArgs);
            ILGenerator ilCtor = ctor.GetILGenerator();
            {
                // valuetype doesn't need to call base ctor
                if (!Descriptor.TypeDef.IsValueType)
                {
                    // ALWAYS call object ctor, *even if* we have an intermediate inherited class
                    // since all generated code does the same thing in its ctor
                    // this is slightly less code, and substantially easier implementation
                    ilCtor.Emit(OpCodes.Ldarg_0);                   // this
                    ilCtor.Emit(OpCodes.Call, Object_Ctor);         // instance void [System.Runtime]System.Object::.ctor()
                }

                ilCtor.Emit(OpCodes.Ldarg_0);                       // this
                ilCtor.Emit(OpCodes.Ldarg_1);                       // memorySource
                ilCtor.Emit(OpCodes.Stfld, fbSource);               // class [Il2CppToolkit.Runtime]Il2CppToolkit.Runtime.IMemorySource Client.App.Application::__source

                ilCtor.Emit(OpCodes.Ldarg_0);                       // this
                ilCtor.Emit(OpCodes.Ldarg_2);                       // address
                ilCtor.Emit(OpCodes.Stfld, fbAddress);              // unsigned int64 Client.App.Application::__address

                ilCtor.Emit(OpCodes.Ret);
            }
        }

        private void InitDerivedType()
        {
            ConstructorInfo baseCtor = TypeBuilder.GetConstructor(CtorArgs);
            ConstructorBuilder ctor = TypeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard | CallingConventions.HasThis, CtorArgs);
            ILGenerator ilCtor = ctor.GetILGenerator();
            ilCtor.Emit(OpCodes.Ldarg_0);                      // this
            ilCtor.Emit(OpCodes.Ldarg_1);                      // memorySource
            ilCtor.Emit(OpCodes.Ldarg_2);                      // address
            ilCtor.Emit(OpCodes.Call, baseCtor);               // instance void base::.ctor(class [Il2CppToolkit.Runtime]Il2CppToolkit.Runtime.IMemorySource, unsigned int64)
            ilCtor.Emit(OpCodes.Ret);
        }


    }
}
using System;
using System.Collections.Generic;
using System.Linq;
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
        ConstructorInfo Ctor { get; }
        Type Type { get; }
        TypeDescriptor Descriptor { get; }
        void Create();
        void Build(BuildTypeResolver resolver, ConstructorCache ctorCache);
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
        public ConstructorInfo Ctor => null;
        public Type Type { get; }
        public TypeDescriptor Descriptor { get; }
        public void Build(BuildTypeResolver resolver, ConstructorCache ctorCache) { }
        public void Create() { }

        public BuiltInType(Type type, TypeDescriptor descriptor)
        {
            Type = type;
            Descriptor = descriptor;
        }
    }

    public class GeneratedType : IGeneratedType
    {
        private TypeBuilder TypeBuilder;
        private bool IsCreated = false;
        private bool IsBuilt = false;

        public ConstructorInfo Ctor { get; private set; }
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

        public void Build(BuildTypeResolver typeResolver, ConstructorCache ctorCache)
        {
            if (IsBuilt)
                return;

            if (Descriptor.TypeDef.IsEnum)
            {
                BuildEnum(typeResolver);
                return;
            }

            if (Descriptor.Base != null)
            {
                TypeBuilder.SetParent(typeResolver.ResolveTypeReference(Descriptor.Base));
            }

            CreateConstructor(typeResolver, ctorCache);

            ConstructorBuilder cctor = TypeBuilder.DefineTypeInitializer();

            ILGenerator cctoril = cctor.GetILGenerator();
            {
                BuildFields(typeResolver, cctoril);
                BuildMethods(typeResolver, cctoril);
            }

            cctoril.Emit(OpCodes.Ret);

            IsBuilt = true;
        }

        #region Methods
        private void BuildMethods(BuildTypeResolver typeResolver, ILGenerator cctoril)
        {
            IList<IGrouping<string, MethodDescriptor>> methodGroups = Descriptor.Methods.GroupBy(method => method.Name).ToList();
            DisambiguateMethodNames(methodGroups);
            foreach (var methodGroup in methodGroups)
            {
                string methodName = methodGroup.Key;
            }
        }

        private static void DisambiguateMethodNames(IList<IGrouping<string, MethodDescriptor>> methodGroups)
        {
            foreach (var group in methodGroups)
            {
                if (group.Count() == 1)
                    continue;
                int idx = 0;
                foreach (var method in group)
                {
                    method.DisambiguatedName = $"{method.Name}_{idx++}";
                }
            }
        }
        #endregion

        #region Fields/Properties
        private const FieldAttributes FieldDefAttrs = FieldAttributes.InitOnly | FieldAttributes.Static | FieldAttributes.Public;
        private void BuildFields(BuildTypeResolver typeResolver, ILGenerator cctoril)
        {
            foreach (var field in Descriptor.Fields)
            {
                Type fieldType = typeResolver.ResolveTypeReference(field.Type) ?? typeof(object);
                if (fieldType == null)
                {
                    CompilerError.UnknownType.Raise($"Dropping field '{field.Name}' from '{TypeBuilder.Name}'. Reason: unknown type");
                    return;
                }

                byte indirection = 1;
                while (fieldType.IsPointer)
                {
                    ++indirection;
                    fieldType = fieldType.GetElementType();
                }

                if (field.Attributes.HasFlag(FieldAttributes.Static))
                {
                    CreateStaticProperty(cctoril, field, fieldType, indirection);
                }
                else
                {
                    CreateMemberProperty(cctoril, field, fieldType, indirection);
                }

            }
        }

        private void CreateStaticProperty(ILGenerator cctoril, FieldDescriptor field, Type fieldType, byte indirection)
        {
            if (Descriptor.TypeInfo == null)
            {
                CompilerError.UnknownType.Raise($"Dropping field '{field.Name}' from '{TypeBuilder.Name}'. Reason: no typeinfo available");
                return;
            }
            Type fieldDefType = typeof(StaticFieldDefinition<>).MakeGenericType(fieldType);
            ConstructorInfo fieldDefCtor;
            try
            {
                fieldDefCtor = fieldDefType.GetConstructor(new Type[] { typeof(string), typeof(ulong), typeof(ulong), typeof(byte) });
            }
            catch
            {
                fieldDefCtor = TypeBuilder.GetConstructor(fieldDefType, typeof(StaticFieldDefinition<>).GetConstructor(new Type[] { typeof(string), typeof(ulong), typeof(ulong), typeof(byte) }));
            }
            FieldBuilder fb = TypeBuilder.DefineField($"s_fieldDef_{field.Name}", fieldDefType, FieldDefAttrs);
            HideFromIntellisense(fb);

            cctoril.Emit(OpCodes.Ldstr, Descriptor.TypeInfo.ModuleName);
            cctoril.Emit(OpCodes.Ldc_I4, (int)Descriptor.TypeInfo.Address);
            cctoril.Emit(OpCodes.Conv_I8);
            cctoril.Emit(OpCodes.Ldc_I4, (int)field.Offset);
            cctoril.Emit(OpCodes.Conv_I8);
            cctoril.Emit(OpCodes.Ldc_I4, indirection);
            cctoril.Emit(OpCodes.Newobj, fieldDefCtor);
            cctoril.Emit(OpCodes.Stsfld, fb);
        }

        private void CreateMemberProperty(ILGenerator cctoril, FieldDescriptor field, Type fieldType, byte indirection)
        {
            Type fieldDefType = typeof(FieldDefinition<,>).MakeGenericType(TypeBuilder, fieldType);
            MethodInfo getValueMethod = TypeBuilder.GetMethod(fieldDefType, typeof(FieldDefinition<,>).GetMethod("GetValue"));
            ConstructorInfo fieldDefCtor = typeof(FieldDefinition<,>).GetConstructor(new Type[] { typeof(ulong), typeof(byte) });
            fieldDefCtor = TypeBuilder.GetConstructor(fieldDefType, fieldDefCtor);
            FieldBuilder fb = TypeBuilder.DefineField($"s_fieldDef_{field.Name}", fieldDefType, FieldDefAttrs);
            HideFromIntellisense(fb);

            cctoril.Emit(OpCodes.Ldc_I4, (int)field.Offset);
            cctoril.Emit(OpCodes.Conv_I8);
            cctoril.Emit(OpCodes.Ldc_I4, indirection);
            cctoril.Emit(OpCodes.Newobj, fieldDefCtor);
            cctoril.Emit(OpCodes.Stsfld, fb);

            MethodBuilder mb = TypeBuilder.DefineMethod($"get_{field.Name}", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.RTSpecialName | MethodAttributes.SpecialName, fieldType, Type.EmptyTypes);
            ILGenerator mbil = mb.GetILGenerator();
            mbil.Emit(OpCodes.Ldsfld, fb);
            mbil.Emit(OpCodes.Ldarg_0);
            mbil.EmitCall(OpCodes.Callvirt, getValueMethod, null);
            mbil.Emit(OpCodes.Ret);

            PropertyBuilder pb = TypeBuilder.DefineProperty(field.Name, PropertyAttributes.None, fieldType, Type.EmptyTypes);
            pb.SetGetMethod(mb);
        }

        private static void HideFromIntellisense(FieldBuilder fb)
        {
            fb.SetCustomAttribute(
                new CustomAttributeBuilder(
                    typeof(System.ComponentModel.EditorBrowsableAttribute)
                    .GetConstructor(new[] { typeof(System.ComponentModel.EditorBrowsableState) }
                ),
                new object[] { System.ComponentModel.EditorBrowsableState.Never }));
        }
        #endregion

        #region Built-in type requirements
        private static readonly Type[] CtorArgs = new Type[] { typeof(IMemorySource) /*source*/, typeof(ulong) /*address*/ };
        private static readonly ConstructorInfo Object_Ctor = typeof(object).GetConstructor(Type.EmptyTypes);
        private const MethodAttributes InstanceGetterAttribs = MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.Virtual | MethodAttributes.RTSpecialName | MethodAttributes.SpecialName | MethodAttributes.NewSlot;

        private void BuildEnum(BuildTypeResolver typeResolver)
        {
            foreach (FieldDescriptor field in Descriptor.Fields)
            {
                FieldBuilder fb = TypeBuilder.DefineField(field.Name, typeResolver.ResolveTypeReference(field.Type), field.Attributes);
                if (field.DefaultValue != null)
                    fb.SetConstant(field.DefaultValue);
            }
        }

        private FieldBuilder AddExplicitIRuntimeObjectFieldImpl<T>(string name, FieldAttributes fieldAttribs)
        {
            FieldBuilder fb = TypeBuilder.DefineField($"<{name}>k__BackingField", typeof(T), FieldAttributes.Private | FieldAttributes.InitOnly);

            MethodBuilder mb = TypeBuilder.DefineMethod($"IRuntimeObject.get_{name}", InstanceGetterAttribs, typeof(T), Type.EmptyTypes);
            ILGenerator mbil = mb.GetILGenerator();
            {
                mbil.Emit(OpCodes.Ldarg_0);
                mbil.Emit(OpCodes.Ldfld, fb);
                mbil.Emit(OpCodes.Ret);
            }

            PropertyBuilder pb = TypeBuilder.DefineProperty($"IRuntimeObject.{name}", PropertyAttributes.None, typeof(T), null);
            pb.SetGetMethod(mb);

            TypeBuilder.DefineMethodOverride(mb, typeof(IRuntimeObject).GetProperty(name).GetGetMethod());

            return fb;
        }

        private void CreateConstructor(BuildTypeResolver typeResolver, ConstructorCache ctorCache)
        {
            if (TypeBuilder.IsInterface || TypeBuilder.IsEnum || Descriptor.IsStatic)
                return;

            ConstructorInfo ctor;
            if (TypeBuilder.BaseType is TypeBuilder)
                ctor = InitDerivedType(typeResolver, ctorCache);
            else
                ctor = InitBaseType();

            ctorCache.Add(Type, ctor);
        }

        private ConstructorInfo InitBaseType()
        {
            TypeBuilder.AddInterfaceImplementation(typeof(IRuntimeObject));

            FieldBuilder fbAddress = AddExplicitIRuntimeObjectFieldImpl<ulong>("Address", FieldAttributes.Private | FieldAttributes.InitOnly);
            FieldBuilder fbSource = AddExplicitIRuntimeObjectFieldImpl<IMemorySource>("Source", FieldAttributes.Private | FieldAttributes.InitOnly);

            ConstructorBuilder cb = TypeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard | CallingConventions.HasThis, CtorArgs);
            Ctor = cb;
            ILGenerator ilCtor = cb.GetILGenerator();
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
            return cb;
        }

        private ConstructorInfo InitDerivedType(BuildTypeResolver typeResolver, ConstructorCache ctorCache)
        {
            Type baseType = typeResolver.ResolveTypeReference(Descriptor.Base);
            if (!ctorCache.TryGetValue(baseType, out ConstructorInfo baseCtor))
                throw new ArgumentOutOfRangeException("Base constructor is referenced before it is created");

            ConstructorBuilder ctor = TypeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard | CallingConventions.HasThis, CtorArgs);
            ILGenerator ilCtor = ctor.GetILGenerator();
            ilCtor.Emit(OpCodes.Ldarg_0);                      // this
            ilCtor.Emit(OpCodes.Ldarg_1);                      // memorySource
            ilCtor.Emit(OpCodes.Ldarg_2);                      // address
            ilCtor.Emit(OpCodes.Call, baseCtor);               // instance void base::.ctor(class [Il2CppToolkit.Runtime]Il2CppToolkit.Runtime.IMemorySource, unsigned int64)
            ilCtor.Emit(OpCodes.Ret);

            return ctor;
        }
        #endregion
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using Il2CppToolkit.Common.Errors;
using Il2CppToolkit.Model;
using Il2CppToolkit.Runtime;
using Il2CppToolkit.Runtime.Types;

namespace Il2CppToolkit.ReverseCompiler
{
    public class DefineTypesPhase : CompilePhase, IResolveTypeFromTypeDefinition
    {
        public override string Name => "Define Types";
        private IReadOnlyList<TypeDescriptor> m_typeDescriptors;

        private CompileContext m_context;

        private string m_moduleName;
        private AssemblyName m_asmName;
        private AssemblyBuilder m_asm;
        private ModuleBuilder m_module;
        private readonly Dictionary<TypeDescriptor, Type> m_generatedTypes = new();
        private BuildTypeResolver m_typeResolver;

        public override async Task Initialize(CompileContext context)
        {
            m_context = context;
            m_moduleName = m_context.Model.ModuleName;
            m_asmName = new AssemblyName(context.Artifacts.Get(ArtifactSpecs.AssemblyName));
            m_typeDescriptors = await context.Artifacts.GetAsync(ArtifactSpecs.SortedTypeDescriptors);
            m_typeResolver = new(context, m_generatedTypes);
        }

        public override Task Execute()
        {
            m_asm = AssemblyBuilder.DefineDynamicAssembly(m_asmName, AssemblyBuilderAccess.RunAndCollect);
            m_asm.SetCustomAttribute(new CustomAttributeBuilder(typeof(GeneratedAttribute).GetConstructor(Type.EmptyTypes), new object[] { }));
            m_module = m_asm.DefineDynamicModule(m_asmName.Name);

            foreach (TypeDescriptor descriptor in m_typeDescriptors)
            {
                EnsureType(descriptor);
            }

            return base.Execute();
        }

        public override Task Finalize()
        {
            m_context.Artifacts.Set(ArtifactSpecs.GeneratedTypes, m_generatedTypes);
            m_context.Artifacts.Set(ArtifactSpecs.GeneratedModule, m_module);
            return base.Finalize();
        }

        public Type EnsureType(TypeDescriptor descriptor)
        {
            if (descriptor == null)
            {
                return null;
            }
            if (m_generatedTypes.TryGetValue(descriptor, out Type value))
            {
                return value;
            }
            return BuildType(descriptor);
        }

        private Type BuildType(TypeDescriptor descriptor)
        {
            Type type = CreateAndRegisterType(descriptor);
            ErrorHandler.VerifyElseThrow(m_generatedTypes.ContainsKey(descriptor), CompilerError.InternalError, "type was not added to m_generatedTypes");

            if (type == null)
            {
                return null;
            }

            if (type is TypeBuilder tb)
            {
                tb.SetCustomAttribute(new CustomAttributeBuilder(
                    typeof(TokenAttribute).GetConstructor(new[] { typeof(uint) }), new object[] { descriptor.TypeDef.token }));
                tb.SetCustomAttribute(new CustomAttributeBuilder(
                    typeof(TagAttribute).GetConstructor(new[] { typeof(ulong) }), new object[] { descriptor.Tag }));
                tb.SetCustomAttribute(new CustomAttributeBuilder(
                    typeof(SizeAttribute).GetConstructor(new[] { typeof(uint) }), new object[] { descriptor.SizeInBytes }));

                if (descriptor.IsStatic)
                {
                    if (m_context.Model.TypeDefToAddress.TryGetValue(descriptor.TypeDef, out ulong address))
                    {
                        tb.SetCustomAttribute(new CustomAttributeBuilder(
                            typeof(AddressAttribute).GetConstructor(new[] { typeof(ulong), typeof(string) }),
                            new object[] { address, m_moduleName }));
                    }
                }

                // enum
                if (descriptor.TypeDef.IsEnum)
                {
                    BuildEnum(descriptor, tb);
                }

                // generics
                if (descriptor.GenericParameterNames.Length > 0)
                {
                    tb.DefineGenericParameters(descriptor.GenericParameterNames);
                }

                // constructor
                if (!descriptor.TypeDef.IsEnum && !descriptor.Attributes.HasFlag(TypeAttributes.Interface))
                {
                    if (descriptor.TypeDef.IsValueType)
                    {
                        tb.DefineDefaultConstructor(MethodAttributes.Public);
                    }
                    else
                    {
                        if (descriptor.IsStatic)
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

            // visit members, don't create them.
            if (!descriptor.TypeDef.IsEnum)
            {
                ResolveTypeReference(descriptor.Base);
                descriptor.Fields.ForEach(field => ResolveTypeReference(field.Type));
                EnsureType(descriptor.GenericParent);
                descriptor.Implements.ForEach(iface => ResolveTypeReference(iface));
            }

            return type;
        }

        private void BuildEnum(TypeDescriptor descriptor, TypeBuilder typeBuilder)
        {
            foreach (FieldDescriptor field in descriptor.Fields)
            {
                FieldBuilder fb = typeBuilder.DefineField(field.Name, ResolveTypeReference(field.Type), field.Attributes);
                if (field.DefaultValue != null)
                {
                    fb.SetConstant(field.DefaultValue);
                }
            }
        }

        private static void CreateConstructor(TypeBuilder tb, Type[] ctorArgs, ConstructorInfo ctorInfo)
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

        private Type GetBaseTypeFromDescriptorIfSimple(TypeDescriptor descriptor)
        {
            if (descriptor?.Base == null)
                return null;

            if (descriptor.Base is DotNetTypeReference dotnet)
                return dotnet.Type;

            if (Types.TryGetType(descriptor.Base.Name, out Type builtInType) && builtInType != null)
                return builtInType;

            return null;
        }

        private Type CreateAndRegisterType(TypeDescriptor descriptor)
        {
            if (Types.TryGetType(descriptor.Name, out Type type))
                return RegisterType(descriptor, type);

            Type baseType = GetBaseTypeFromDescriptorIfSimple(descriptor);
            if (descriptor.DeclaringParent != null)
            {
                type = EnsureType(descriptor.DeclaringParent);
                if (type == null)
                    return RegisterType(descriptor, null);

                // exclude types declared within a built-in type 
                if (type is not TypeBuilder parentBuilder)
                    return RegisterType(descriptor, null);

                return RegisterType(
                    descriptor,
                    parentBuilder.DefineNestedType(descriptor.Name, descriptor.Attributes, baseType)
                    );
            }
            return RegisterType(
                descriptor,
                m_module.DefineType(descriptor.Name, descriptor.Attributes, baseType)
                );
        }

        private Type RegisterType(TypeDescriptor descriptor, Type type)
        {
            m_generatedTypes.Add(descriptor, type);
            return type;
        }

        private Type ResolveTypeReference(ITypeReference reference)
        {
            return m_typeResolver.ResolveTypeReference(reference, this);
        }

        private static readonly Dictionary<int, Type> TypeMap = new()
        {
            { 1, typeof(void) },
            { 2, typeof(bool) },
            { 3, typeof(char) },
            { 4, typeof(sbyte) },
            { 5, typeof(byte) },
            { 6, typeof(short) },
            { 7, typeof(ushort) },
            { 8, typeof(int) },
            { 9, typeof(uint) },
            { 10, typeof(long) },
            { 11, typeof(ulong) },
            { 12, typeof(float) },
            { 13, typeof(double) },
            { 14, typeof(string) },
            { 22, typeof(IntPtr) },
            { 24, typeof(IntPtr) },
            { 25, typeof(UIntPtr) },
            { 28, typeof(object) },
        };

    }

    internal class StaticReflectionHandles
    {
        public static class MethodDefinition
        {
            public static class Ctor
            {
                public static readonly System.Type[] Parameters = { typeof(ulong), typeof(string) };
                public static readonly ConstructorInfo ConstructorInfo = typeof(Il2CppToolkit.Runtime.Types.Reflection.MethodDefinition).GetConstructor(
                    BindingFlags.Public | BindingFlags.Instance,
                    null,
                    Parameters,
                    null);
            }
        }

        public static class Type
        {
            public static readonly MethodInfo GetTypeFromHandle = typeof(System.Type).GetMethod("GetTypeFromHandle");
            public static readonly MethodInfo op_Equality =
                typeof(System.Type).GetMethod("op_Equality", BindingFlags.Static | BindingFlags.Public);
        }

        public static class StructBase
        {
            public static readonly MethodInfo Load =
                typeof(Il2CppToolkit.Runtime.StructBase).GetMethod("Load", BindingFlags.NonPublic | BindingFlags.Instance);

            public static class Ctor
            {
                public static readonly System.Type[] Parameters = { typeof(IMemorySource), typeof(ulong) };
                public static readonly ConstructorInfo ConstructorInfo = typeof(Il2CppToolkit.Runtime.StructBase).GetConstructor(
                    BindingFlags.NonPublic | BindingFlags.Instance,
                    null,
                    Parameters,
                    null
                    );
            }
        }

        public static class StaticInstance
        {
            public static class Ctor
            {
                public static System.Type[] Parameters = StructBase.Ctor.Parameters;
                public static readonly ConstructorInfo ConstructorInfo = typeof(Il2CppToolkit.Runtime.StaticInstance<>).GetConstructor(
                    BindingFlags.NonPublic | BindingFlags.Instance,
                    null,
                    Parameters,
                    null
                    );
            }
        }
    }
}

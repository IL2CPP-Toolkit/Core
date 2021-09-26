using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using System.Threading.Tasks;
using Il2CppToolkit.Common.Errors;
using Il2CppToolkit.Model;
using Il2CppToolkit.Runtime;
using Il2CppToolkit.Runtime.Types;

namespace Il2CppToolkit.ReverseCompiler
{
    public class BuildTypesPhase : CompilePhase
    {
        public override string Name => "Build Types";
        private IReadOnlyList<TypeDescriptor> m_typeDescriptors;

        private CompileContext m_context;

        private string m_moduleName;
        private AssemblyName m_asmName;
        private AssemblyBuilder m_asm;
        private ModuleBuilder m_module;
        private readonly Dictionary<TypeDescriptor, Type> m_generatedTypes = new();
        private readonly Dictionary<string, Type> m_generatedTypeByFullName = new();
        private readonly Dictionary<string, List<Type>> m_generatedTypeByClassName = new();

        public override async Task Initialize(CompileContext context)
        {
            m_context = context;
            m_moduleName = m_context.Model.ModuleName;
            m_asmName = new AssemblyName(context.Artifacts.Get(ArtifactSpecs.AssemblyName));
            m_typeDescriptors = await context.Artifacts.GetAsync(ArtifactSpecs.SortedTypeDescriptors);
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

        private Type EnsureType(TypeDescriptor descriptor)
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
            m_generatedTypeByFullName.Add(descriptor.FullName, type);
            if (!m_generatedTypeByClassName.ContainsKey(descriptor.Name))
            {
                m_generatedTypeByClassName.Add(descriptor.Name, new List<Type>());
            }
            m_generatedTypeByClassName[descriptor.Name].Add(type);
            return type;
        }

        private Type ResolveTypeReference(ITypeReference reference)
        {
            if (reference == null)
            {
                return null;
            }

            switch (reference)
            {
                case DotNetTypeReference dotnet: return dotnet.Type;
                case TypeDescriptorReference typeRef: return m_generatedTypes[typeRef.Descriptor];
                case GenericTypeReference genericTypeRef:
                    {
                        Type[] typeArgs = genericTypeRef.TypeArguments.Select(ResolveTypeReference).ToArray();
                        Type specializedType = ResolveTypeReference(genericTypeRef.GenericType).MakeGenericType(typeArgs);
                        return specializedType;
                    }
                case Il2CppTypeReference cppType: return ResolveTypeReference(cppType.CppType, cppType.TypeContext);
                default:
                    CompilerError.UnknownTypeReference.Throw("Unsupported type reference");
                    return null;
            }
        }

        private Type ResolveTypeReference(Il2CppType il2CppType, TypeDescriptor typeContext)
        {
            string typeName = m_context.Model.GetTypeName(il2CppType, true, false);
            switch (il2CppType.type)
            {
                case Il2CppTypeEnum.IL2CPP_TYPE_ARRAY:
                    {
                        Il2CppArrayType arrayType = m_context.Model.Il2Cpp.MapVATR<Il2CppArrayType>(il2CppType.data.array);
                        Il2CppType elementCppType = m_context.Model.Il2Cpp.GetIl2CppType(arrayType.etype);
                        Type elementType = ResolveTypeReference(elementCppType, typeContext);
                        return elementType?.MakeArrayType(arrayType.rank);
                    }
                case Il2CppTypeEnum.IL2CPP_TYPE_SZARRAY:
                    {
                        Il2CppType elementCppType = m_context.Model.Il2Cpp.GetIl2CppType(il2CppType.data.type);
                        Type elementType = ResolveTypeReference(elementCppType, typeContext);
                        return elementType?.MakeArrayType();
                    }
                case Il2CppTypeEnum.IL2CPP_TYPE_PTR:
                    {
                        Il2CppType oriType = m_context.Model.Il2Cpp.GetIl2CppType(il2CppType.data.type);
                        Type ptrToType = ResolveTypeReference(oriType, typeContext);
                        return ptrToType?.MakePointerType();
                    }
                case Il2CppTypeEnum.IL2CPP_TYPE_VAR:
                case Il2CppTypeEnum.IL2CPP_TYPE_MVAR:
                    {
                        // TODO: Is this even remotely correct? :S
                        Il2CppGenericParameter param = m_context.Model.GetGenericParameterFromIl2CppType(il2CppType);
                        Type type = m_generatedTypes[typeContext];
                        return (type as TypeInfo)?.GenericTypeParameters[param.num];
                    }
                case Il2CppTypeEnum.IL2CPP_TYPE_CLASS:
                case Il2CppTypeEnum.IL2CPP_TYPE_VALUETYPE:
                    {
                        Il2CppTypeDefinition typeDef = m_context.Model.GetTypeDefinitionFromIl2CppType(il2CppType);
                        int typeDefIndex = Array.IndexOf(m_context.Model.Metadata.typeDefs, typeDef);
                        return EnsureType(m_context.Model.TypeDefsByIndex[typeDefIndex]);
                    }
                case Il2CppTypeEnum.IL2CPP_TYPE_GENERICINST:
                    {
                        Il2CppGenericClass genericClass = m_context.Model.Il2Cpp.MapVATR<Il2CppGenericClass>(il2CppType.data.generic_class);
                        Il2CppTypeDefinition genericTypeDef = m_context.Model.GetGenericClassTypeDefinition(genericClass);
                        Il2CppGenericInst genericInst = m_context.Model.Il2Cpp.MapVATR<Il2CppGenericInst>(genericClass.context.class_inst);
                        List<Type> genericParameterTypes = new();
                        ulong[] pointers = m_context.Model.Il2Cpp.MapVATR<ulong>(genericInst.type_argv, genericInst.type_argc);
                        for (int i = 0; i < genericInst.type_argc; i++)
                        {
                            Il2CppType paramCppType = m_context.Model.Il2Cpp.GetIl2CppType(pointers[i]);
                            Type ptype = ResolveTypeReference(paramCppType, typeContext);
                            if (ptype == null)
                            {
                                CompilerError.IncompleteGenericType.Throw($"Dropping '{typeName}'. Reason: incomplete generic type");
                                return null;
                            }
                            genericParameterTypes.Add(ptype);
                        }

                        int typeDefIndex = Array.IndexOf(m_context.Model.Metadata.typeDefs, genericTypeDef);
                        return EnsureType(m_context.Model.TypeDefsByIndex[typeDefIndex])?.MakeGenericType(genericParameterTypes.ToArray());
                    }
                default:
                    return TypeMap[(int)il2CppType.type];
            }
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
                public static readonly System.Type[] Parameters = { typeof(Il2CsRuntimeContext), typeof(ulong) };
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

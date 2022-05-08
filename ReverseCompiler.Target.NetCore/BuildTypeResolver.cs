using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Il2CppToolkit.Model;

namespace Il2CppToolkit.ReverseCompiler.Target.NetCore
{
    public interface IResolveTypeFromTypeDefinition
    {
        Type EnsureType(TypeDescriptor descriptor);
    }

    public class BuildTypeResolver
    {
        private readonly CompileContext m_context;
        private readonly IReadOnlyDictionary<TypeDescriptor, IGeneratedType> m_generatedTypeMap;

        public BuildTypeResolver(CompileContext context, IReadOnlyDictionary<TypeDescriptor, IGeneratedType> generatedTypeMap)
        {
            m_context = context;
            m_generatedTypeMap = generatedTypeMap;
        }

        public Type TryEnsureType(TypeDescriptor descriptor, IResolveTypeFromTypeDefinition resolver)
        {
            if (m_generatedTypeMap.TryGetValue(descriptor, out IGeneratedType type))
            {
                return type.Type;
            }
            return resolver?.EnsureType(descriptor);
        }

        public ISet<TypeDescriptor> GetGeneratedDependents(ITypeReference reference, HashSet<TypeDescriptor> set = null)
        {
            set ??= new();
            if (reference == null)
                return set;

            switch (reference)
            {
                case DotNetTypeReference dotnet: return set;
                case TypeDescriptorReference typeRef:
                    {
                        set.Add(typeRef.Descriptor);
                        return set;
                    }
                case GenericTypeReference genericTypeRef:
                    {
                        foreach (var arg in genericTypeRef.TypeArguments)
                            GetGeneratedDependents(arg, set);
                        return set;
                    }
                case Il2CppTypeReference cppType:
                    {
                        ResolveCppTypeDependents(cppType.CppType, cppType.TypeContext, set);
                        return set;
                    }
                default:
                    CompilerError.UnknownTypeReference.Raise("Unsupported type reference");
                    return set;
            }
        }

        private void ResolveCppTypeDependents(Il2CppType il2CppType, TypeDescriptor typeContext, HashSet<TypeDescriptor> set)
        {
            string shortTypeNameForLogging = m_context.Model.GetTypeName(il2CppType, false, false);
            switch (il2CppType.type)
            {
                case Il2CppTypeEnum.IL2CPP_TYPE_ARRAY:
                    {
                        Il2CppArrayType arrayType = m_context.Model.Il2Cpp.MapVATR<Il2CppArrayType>(il2CppType.data.array);
                        Il2CppType elementCppType = m_context.Model.Il2Cpp.GetIl2CppType(arrayType.etype);
                        ResolveCppTypeDependents(elementCppType, typeContext, set);
                        return;
                    }
                case Il2CppTypeEnum.IL2CPP_TYPE_SZARRAY:
                    {
                        Il2CppType elementCppType = m_context.Model.Il2Cpp.GetIl2CppType(il2CppType.data.type);
                        ResolveCppTypeDependents(elementCppType, typeContext, set);
                        return;
                    }
                case Il2CppTypeEnum.IL2CPP_TYPE_PTR:
                    {
                        Il2CppType oriType = m_context.Model.Il2Cpp.GetIl2CppType(il2CppType.data.type);
                        ResolveCppTypeDependents(oriType, typeContext, set);
                        return;
                    }
                case Il2CppTypeEnum.IL2CPP_TYPE_VAR:
                case Il2CppTypeEnum.IL2CPP_TYPE_MVAR:
                    {
                        // TODO: Is this even remotely correct? :S
                        Il2CppGenericParameter param = m_context.Model.GetGenericParameterFromIl2CppType(il2CppType);
                        set.Add(typeContext);
                        return;
                    }
                case Il2CppTypeEnum.IL2CPP_TYPE_CLASS:
                case Il2CppTypeEnum.IL2CPP_TYPE_VALUETYPE:
                    {
                        Il2CppTypeDefinition typeDef = m_context.Model.GetTypeDefinitionFromIl2CppType(il2CppType);
                        int typeDefIndex = Array.IndexOf(m_context.Model.Metadata.typeDefs, typeDef);
                        set.Add(m_context.Model.TypeDefsByIndex[typeDefIndex]);
                        return;
                    }
                case Il2CppTypeEnum.IL2CPP_TYPE_GENERICINST:
                    {
                        Il2CppGenericClass genericClass = m_context.Model.Il2Cpp.MapVATR<Il2CppGenericClass>(il2CppType.data.generic_class);
                        Il2CppTypeDefinition genericTypeDef = m_context.Model.GetGenericClassTypeDefinition(genericClass);
                        int typeDefIndex = Array.IndexOf(m_context.Model.Metadata.typeDefs, genericTypeDef);
                        set.Add(m_context.Model.TypeDefsByIndex[typeDefIndex]);
                        return;
                    }
                default:
                    return;
            }
        }

        public Type ResolveTypeReference(ITypeReference reference, IResolveTypeFromTypeDefinition resolver = null)
        {
            if (reference == null)
            {
                return null;
            }

            switch (reference)
            {
                case DotNetTypeReference dotnet: return dotnet.Type;
                case TypeDescriptorReference typeRef: return TryEnsureType(typeRef.Descriptor, resolver);
                case GenericTypeReference genericTypeRef:
                    {
                        Type[] typeArgs = genericTypeRef.TypeArguments.Select(arg => ResolveTypeReference(arg, resolver)).ToArray();
                        Type specializedType = ResolveTypeReference(genericTypeRef.GenericType).MakeGenericType(typeArgs);
                        return specializedType;
                    }
                case Il2CppTypeReference cppType: return ResolveCppTypeReference(cppType.CppType, cppType.TypeContext, resolver);
                default:
                    CompilerError.UnknownTypeReference.Raise("Unsupported type reference");
                    return null;
            }
        }

        private Type ResolveCppTypeReference(Il2CppType il2CppType, TypeDescriptor typeContext, IResolveTypeFromTypeDefinition resolver)
        {
            string shortTypeNameForLogging = m_context.Model.GetTypeName(il2CppType, false, false);
            switch (il2CppType.type)
            {
                case Il2CppTypeEnum.IL2CPP_TYPE_ARRAY:
                    {
                        Il2CppArrayType arrayType = m_context.Model.Il2Cpp.MapVATR<Il2CppArrayType>(il2CppType.data.array);
                        Il2CppType elementCppType = m_context.Model.Il2Cpp.GetIl2CppType(arrayType.etype);
                        Type elementType = ResolveCppTypeReference(elementCppType, typeContext, resolver);
                        return elementType?.MakeArrayType(arrayType.rank);
                    }
                case Il2CppTypeEnum.IL2CPP_TYPE_SZARRAY:
                    {
                        Il2CppType elementCppType = m_context.Model.Il2Cpp.GetIl2CppType(il2CppType.data.type);
                        Type elementType = ResolveCppTypeReference(elementCppType, typeContext, resolver);
                        return elementType?.MakeArrayType();
                    }
                case Il2CppTypeEnum.IL2CPP_TYPE_PTR:
                    {
                        Il2CppType oriType = m_context.Model.Il2Cpp.GetIl2CppType(il2CppType.data.type);
                        Type ptrToType = ResolveCppTypeReference(oriType, typeContext, resolver);
                        return ptrToType?.MakePointerType();
                    }
                case Il2CppTypeEnum.IL2CPP_TYPE_VAR:
                case Il2CppTypeEnum.IL2CPP_TYPE_MVAR:
                    {
                        // TODO: Is this even remotely correct? :S
                        Il2CppGenericParameter param = m_context.Model.GetGenericParameterFromIl2CppType(il2CppType);
                        Type type = m_generatedTypeMap[typeContext].Type;
                        return (type as TypeInfo)?.GenericTypeParameters[param.num];
                    }
                case Il2CppTypeEnum.IL2CPP_TYPE_CLASS:
                case Il2CppTypeEnum.IL2CPP_TYPE_VALUETYPE:
                    {
                        Il2CppTypeDefinition typeDef = m_context.Model.GetTypeDefinitionFromIl2CppType(il2CppType);
                        int typeDefIndex = Array.IndexOf(m_context.Model.Metadata.typeDefs, typeDef);
                        return TryEnsureType(m_context.Model.TypeDefsByIndex[typeDefIndex], resolver);
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
                            Type ptype = ResolveCppTypeReference(paramCppType, typeContext, resolver);
                            if (ptype == null)
                            {
                                CompilerError.IncompleteGenericType.Raise($"One or more generic type parameters are incomplete or excluded. Type: {shortTypeNameForLogging}");
                                return null;
                            }
                            genericParameterTypes.Add(ptype);
                        }

                        int typeDefIndex = Array.IndexOf(m_context.Model.Metadata.typeDefs, genericTypeDef);
                        return TryEnsureType(m_context.Model.TypeDefsByIndex[typeDefIndex], resolver)?.MakeGenericType(genericParameterTypes.ToArray());
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
}
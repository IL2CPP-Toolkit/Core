using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Threading.Tasks;
using Il2CppToolkit.Common.Errors;
using Il2CppToolkit.Model;

namespace Il2CppToolkit.ReverseCompiler
{
    public class TypeGenerationPhase : CompilePhase
    {
        public override string Name => "Type Generation";

        private readonly Dictionary<TypeDescriptor, Type> m_generatedTypes = new();
        private readonly Dictionary<string, Type> m_generatedTypeByFullName = new();
        private readonly Dictionary<string, List<Type>> m_generatedTypeByClassName = new();

        private CompileContext m_context;
        private List<Func<TypeDescriptor, bool>> m_typeSelectors;

        public override async Task Prologue(CompileContext context)
        {
            m_typeSelectors = await context.GetArtifact<List<Func<TypeDescriptor, bool>>>("TypeSelectors");
        }

        public override Task Execute(CompileContext context)
        {
            m_context = context;
            return Task.Run(() =>
            {
                foreach (TypeDescriptor descriptor in SortTypes(FilterTypes(m_typeSelectors)))
                {
                    BuildType(descriptor);
                }
            });
        }

        private Type BuildType(TypeDescriptor descriptor)
        {
            throw new NotImplementedException();
        }

        /**
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
                    if (m_typeDefToAddress.TryGetValue(descriptor.TypeDef, out ulong address))
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

        private Type CreateAndRegisterType(TypeDescriptor descriptor)
        {
            if (Types.TryGetType(descriptor.Name, out Type type))
            {
                return RegisterType(descriptor, type);
            }

            Type baseType = descriptor.Base?.Type;
            if (descriptor.DeclaringParent != null)
            {
                type = EnsureType(descriptor.DeclaringParent);
                if (type == null)
                {
                    return RegisterType(descriptor, null);
                }

                if (type is not TypeBuilder parentBuilder)
                {
                    // exclude types declared within a built-in type 
                    return RegisterType(descriptor, null);
                }
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
        /**/

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

        private IEnumerable<TypeDescriptor> FilterTypes(List<Func<TypeDescriptor, bool>> typeSelectors)
        {
            if (typeSelectors == null || typeSelectors.Count == 0)
            {
                return m_context.Model.TypeDescriptors;
            }
            return m_context.Model.TypeDescriptors.Where(descriptor =>
            {
                foreach (Func<TypeDescriptor, bool> selector in typeSelectors)
                {
                    if (selector(descriptor))
                    {
                        return true;
                    }
                }
                return false;
            });
        }

        private IEnumerable<TypeDescriptor> SortTypes(IEnumerable<TypeDescriptor> types)
        {
            HashSet<TypeDescriptor> queuedSet = new();
            Queue<TypeDescriptor> reopenList = new(types);
            do
            {
                Queue<TypeDescriptor> openList = reopenList;
                reopenList = new();
                while (openList.TryDequeue(out TypeDescriptor td))
                {
                    ErrorHandler.VerifyElseThrow(!queuedSet.Contains(td), CompilerError.InternalError, "Internal error");
                    if (td.DeclaringParent != null && !queuedSet.Contains(td.DeclaringParent))
                    {
                        reopenList.Enqueue(td);
                        continue;
                    }
                    if (td.GenericParent != null && !queuedSet.Contains(td.GenericParent))
                    {
                        reopenList.Enqueue(td);
                        continue;
                    }
                    yield return td;
                    queuedSet.Add(td);
                }
            }
            while (reopenList.Count > 0);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using Il2CppToolkit.Common.Errors;
using Il2CppToolkit.Model;
using Il2CppToolkit.Runtime;
using Il2CppToolkit.Runtime.Types.Reflection;

namespace Il2CppToolkit.ReverseCompiler
{
    public class BuildTypesPhase : CompilePhase
    {
        public override string Name => "Build Types";

        private CompileContext m_context;
        private IReadOnlyDictionary<TypeDescriptor, Type> m_generatedTypes;
        private BuildTypeResolver m_typeResolver;

        public override async Task Initialize(CompileContext context)
        {
            m_context = context;
            m_generatedTypes = await m_context.Artifacts.GetAsync(ArtifactSpecs.GeneratedTypes);
            m_typeResolver = new(context, m_generatedTypes);
        }

        public override Task Execute()
        {
            return Task.CompletedTask;
        }

        public override Task Finalize()
        {
            return Task.CompletedTask;
        }

        private void ProcessTypes()
        {
            foreach ((TypeDescriptor td, Type type) in m_generatedTypes)
            {
                if (type is TypeBuilder tb)
                {
                    if (td.TypeDef.IsEnum)
                        continue;

                    ProcessType(td, tb);
                }
            }
        }

        private Type ResolveTypeReference(ITypeReference reference)
        {
            return m_typeResolver.ResolveTypeReference(reference);
        }

        private ITypeReference GetBaseReferenceTypeFor(TypeDescriptor td)
        {
            if (td.Base != null)
            {
                return td.Base;
            }
            if (td.IsStatic)
            {
                return new GenericTypeReference(new DotNetTypeReference(typeof(StaticInstance<>)), new TypeDescriptorReference(td));
            }

            return new DotNetTypeReference(typeof(StructBase));
        }

        private void ProcessType(TypeDescriptor td, TypeBuilder tb)
        {
            ITypeReference baseTypeRef = GetBaseReferenceTypeFor(td);
            if (baseTypeRef != null)
            {
                tb.SetParent(ResolveTypeReference(td.Base));
            }

            foreach (ITypeReference implements in td.Implements)
            {
                Type interfaceType = ResolveTypeReference(implements);
                if (interfaceType == null)
                {
                    CompilerError.InterfaceNotSupportedOrEmitted.Raise($"Dropping interface '{implements.Name}' from '{td.FullName}': interface is not supported or not emitted.");
                    continue;
                }
                tb.AddInterfaceImplementation(interfaceType);
            }

            foreach (FieldDescriptor field in td.Fields)
            {
                ProcessField(td, tb, field);
            }

            // TODO: Figure out how to map properties<>fields with reasonable accuracy
            //if (td.Attributes.HasFlag(TypeAttributes.Interface))
            //{
            //	foreach (PropertyDescriptor property in td.Properties)
            //	{
            //		ProcessProperty(tb, property);
            //	}
            //}

            // methods on value types not yet supported
            if (!td.TypeDef.IsValueType)
            {
                ProcessMethods(tb, td);
            }
        }

        private void ProcessMethods(TypeBuilder tb, TypeDescriptor td)
        {
            IEnumerable<IGrouping<string, MethodDescriptor>> methodGroups = td.Methods.GroupBy(method => method.Name);
            foreach (IGrouping<string, MethodDescriptor> methodGroup in methodGroups)
            {
                string methodName = methodGroup.Key;
                // only none or single-argument generic type methods are supported right one
                MethodDescriptor[] methods = methodGroup.Where(method => method.DeclaringTypeArgs.Count <= 1).ToArray();
                if (methods.Length == 0)
                    continue;

                Type[] genericTypeParams = ((TypeInfo)((Type)tb)).GenericTypeParameters;
                if (methods.Length > 1 && genericTypeParams.Length == 0)
                    continue; // not supported

                MethodBuilder mb = tb.DefineMethod($"get_method_{methodGroup.Key}",
                    MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
                    typeof(MethodDefinition), Type.EmptyTypes);
                ILGenerator mbil = mb.GetILGenerator();
                if (methods.Length > 1)
                {
                    Type typeT0 = genericTypeParams[0];

                    Label? nextLabel = null;
                    foreach (MethodDescriptor method in methods)
                    {
                        if (method.DeclaringTypeArgs.Count == 0)
                            continue;
                        Type declaringTypeArg0 = ResolveTypeReference(method.DeclaringTypeArgs[0]);
                        // cannot generate types for generic arguments. feel free to implement if you're brave.
                        if (declaringTypeArg0 == null || declaringTypeArg0.IsGenericType)
                            continue;

                        if (nextLabel.HasValue)
                        {
                            mbil.MarkLabel(nextLabel.Value);
                        }

                        nextLabel = mbil.DefineLabel();
                        // if (typeof(T) == typeof(U)) 
                        mbil.Emit(OpCodes.Ldtoken, declaringTypeArg0);
                        mbil.EmitCall(OpCodes.Call, StaticReflectionHandles.Type.GetTypeFromHandle, null);
                        mbil.Emit(OpCodes.Ldtoken, typeT0);
                        mbil.EmitCall(OpCodes.Call, StaticReflectionHandles.Type.GetTypeFromHandle, null);
                        mbil.EmitCall(OpCodes.Call, StaticReflectionHandles.Type.op_Equality, null);
                        mbil.Emit(OpCodes.Brfalse_S, nextLabel.Value);

                        // true -> return new MethodDefinition(address, moduleName)
                        mbil.Emit(OpCodes.Ldc_I8, (long)method.Address);
                        mbil.Emit(OpCodes.Ldstr, m_context.Model.ModuleName);
                        mbil.Emit(OpCodes.Newobj, StaticReflectionHandles.MethodDefinition.Ctor.ConstructorInfo);
                        mbil.Emit(OpCodes.Ret);
                    }
                    ErrorHandler.VerifyElseThrow(nextLabel.HasValue, CompilerError.ILGenerationError, "Internal error: Missing label");
                    mbil.MarkLabel(nextLabel.Value);
                    mbil.Emit(OpCodes.Ldnull);
                    mbil.Emit(OpCodes.Ret);
                }
                else
                {
                    mbil.Emit(OpCodes.Ldc_I8, (long)methods[0].Address);
                    mbil.Emit(OpCodes.Ldstr, m_context.Model.ModuleName);
                    mbil.Emit(OpCodes.Newobj, StaticReflectionHandles.MethodDefinition.Ctor.ConstructorInfo);
                    mbil.Emit(OpCodes.Ret);
                }

                PropertyBuilder pb = tb.DefineProperty($"method_{methodName}", PropertyAttributes.None, typeof(MethodDefinition), null);
                pb.SetGetMethod(mb);
            }
        }

        private void ProcessProperty(TypeBuilder tb, PropertyDescriptor property)
        {
            if (property.GetMethodAttributes.HasFlag(MethodAttributes.Static))
            {
                // TODO
                return;
            }

            Type fieldType = ResolveTypeReference(property.Type);
            if (fieldType == null)
            {
                CompilerError.UnknownType.Raise($"Dropping property '{property.Name}' from '{tb.Name}'. Reason: unknown type");
                return;
            }

            MethodBuilder mb = tb.DefineMethod($"get_{property.Name}", property.GetMethodAttributes, fieldType, Type.EmptyTypes);
            PropertyBuilder pb = tb.DefineProperty(property.Name, PropertyAttributes.None, fieldType, null);
            pb.SetGetMethod(mb);
        }

        private void ProcessField(TypeDescriptor td, TypeBuilder tb, FieldDescriptor field)
        {
            if (field.Attributes.HasFlag(FieldAttributes.Static) != td.IsStatic)
            {
                // TODO
                return;
            }

            Type fieldType = ResolveTypeReference(field.Type);
            if (fieldType == null)
            {
                CompilerError.UnknownType.Raise($"Dropping field '{field.Name}' from '{tb.Name}'. Reason: unknown type");
                return;
            }

            bool generateFieldsOnly = tb.IsValueType;
            byte indirection = 1;
            while (fieldType.IsPointer)
            {
                ++indirection;
                fieldType = fieldType.GetElementType();
            }

            string fieldName = generateFieldsOnly ? field.Name : field.StorageName;
            FieldAttributes fieldAttrs = field.Attributes & ~(FieldAttributes.InitOnly | FieldAttributes.Public | FieldAttributes.Private | FieldAttributes.PrivateScope | FieldAttributes.Static); // TODO: Allow static once we support both types on each class type
            fieldAttrs |= generateFieldsOnly ? FieldAttributes.Public : FieldAttributes.Private;

            FieldBuilder fb = tb.DefineField(fieldName, fieldType, fieldAttrs);

            fb.SetCustomAttribute(new CustomAttributeBuilder(typeof(OffsetAttribute).GetConstructor(new[] { typeof(ulong) }), new object[] { field.Offset }));
            if (indirection > 1)
            {
                fb.SetCustomAttribute(new CustomAttributeBuilder(typeof(IndirectionAttribute).GetConstructor(new[] { typeof(byte) }), new object[] { indirection }));
            }

            // structs only get fields and attributes, nothing more.
            if (generateFieldsOnly)
            {
                return;
            }

            if (field.DefaultValue != null)
            {
                fb.SetConstant(field.DefaultValue);
            }

            MethodBuilder mb = tb.DefineMethod($"get_{field.Name}", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.RTSpecialName | MethodAttributes.SpecialName, fieldType, Type.EmptyTypes);
            ILGenerator mbil = mb.GetILGenerator();
            mbil.Emit(OpCodes.Ldarg_0);
            mbil.Emit(OpCodes.Call, StaticReflectionHandles.StructBase.Load);
            mbil.Emit(OpCodes.Ldarg_0);
            mbil.Emit(OpCodes.Ldfld, fb);
            mbil.Emit(OpCodes.Ret);

            PropertyBuilder pb = tb.DefineProperty(field.Name, PropertyAttributes.None, fieldType, null);
            pb.SetGetMethod(mb);
        }
    }
}

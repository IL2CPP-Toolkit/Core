using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Il2CppToolkit.Common.Errors;
using Il2CppToolkit.Model;
using Il2CppToolkit.Runtime.Types;
using Mono.Cecil;

namespace Il2CppToolkit.ReverseCompiler.Target.NetCore
{
	public class DefineTypesPhase : CompilePhase//, IResolveTypeFromTypeDefinition
	{
		public override string Name => "Define Types";
		private IReadOnlyList<TypeDescriptor> m_typeDescriptors;

		private ICompileContext m_context;

		private string m_asmName;
		private Version m_asmVersion;
		private AssemblyDefinition m_asm;
		private ModuleDefinition m_module;
		private readonly Dictionary<TypeDescriptor, IGeneratedType> m_generatedTypes = new();
		private HashSet<TypeDescriptor> m_pendingDescriptors = new();
		// private BuildTypeResolver m_typeResolver;

		public override async Task Initialize(ICompileContext context)
		{
			m_context = context;

			m_typeDescriptors = await context.Artifacts.GetAsync(NetCoreArtifactSpecs.SortedTypeDescriptors);
			// m_typeResolver = new(context, m_generatedTypes);
			m_asmName = context.Artifacts.Get(ArtifactSpecs.AssemblyName);
			m_asmVersion = context.Artifacts.Get(ArtifactSpecs.AssemblyVersion);
		}

		public override Task Execute()
		{
			AssemblyNameDefinition asmName = new(m_asmName, m_asmVersion);
			m_asm = AssemblyDefinition.CreateAssembly(asmName, m_context.Model.ModuleName, ModuleKind.Dll);
			m_module = m_asm.MainModule;

			foreach (TypeDescriptor descriptor in m_typeDescriptors)
			{
				EnsureType(descriptor);
			}

			while (m_pendingDescriptors.Count > 0)
			{
				var descriptors = m_pendingDescriptors;
				m_pendingDescriptors = new();

				foreach (TypeDescriptor descriptor in descriptors)
				{
					VisitMembers(descriptor);
				}
			}

			return base.Execute();
		}

		public override Task Finalize()
		{
			m_context.Artifacts.Set(NetCoreArtifactSpecs.GeneratedTypes, m_generatedTypes);
			m_context.Artifacts.Set(NetCoreArtifactSpecs.GeneratedModule, m_module);
			return base.Finalize();
		}

		public IGeneratedType EnsureType(TypeDescriptor descriptor)
		{
			if (descriptor == null)
			{
				return null;
			}
			if (m_generatedTypes.TryGetValue(descriptor, out IGeneratedType value))
			{
				return value;
			}
			return DefineAndRegisterType(descriptor);
		}

		private IGeneratedType DefineType(TypeDescriptor descriptor)
		{
			ErrorHandler.VerifyElseThrow(!m_generatedTypes.ContainsKey(descriptor), CompilerError.InternalError, "type was already added to m_generatedTypes");

			if (Types.TryGetType(descriptor.Name, out Type type))
			{
				// TODO: return a `BuiltInType`?
				return null;
			}

			if (descriptor.DeclaringParent != null)
			{
				IGeneratedType parentType = EnsureType(descriptor.DeclaringParent);
				// exclude types declared within a missing or built-in type 
				if (parentType == null || parentType.Excluded)
					return null;

				if (parentType.Excluded)
					return null;
			}
			return GeneratedTypeFactory.Make(descriptor);
		}

		private void VisitMembers(TypeDescriptor descriptor)
		{
			// if (!descriptor.TypeDef.IsEnum)
			// {
			//     ResolveTypeReference(descriptor.Base);
			//     descriptor.Fields.ForEach(field => ResolveTypeReference(field.Type));
			//     EnsureType(descriptor.GenericParent);
			//     descriptor.Implements.ForEach(iface => ResolveTypeReference(iface));
			// }
		}

		private IGeneratedType DefineAndRegisterType(TypeDescriptor descriptor)
		{
			IGeneratedType generatedType = DefineType(descriptor);
			return RegisterType(descriptor, generatedType);
		}

		private IGeneratedType RegisterType(TypeDescriptor descriptor, IGeneratedType type)
		{
			if (m_generatedTypes.TryAdd(descriptor, type))
			{
				m_pendingDescriptors.Add(descriptor);
			}
			return type;
		}

		// private Type ResolveTypeReference(ITypeReference reference)
		// {
		//     return m_typeResolver.ResolveTypeReference(reference, this);
		// }
	}

	// internal class StaticReflectionHandles
	// {
	//     public static class MethodDefinition
	//     {
	//         public static class Ctor
	//         {
	//             public static readonly System.Type[] Parameters = { typeof(ulong), typeof(string) };
	//             public static readonly ConstructorInfo ConstructorInfo = typeof(Il2CppToolkit.Runtime.Types.Reflection.MethodDefinition).GetConstructor(
	//                 BindingFlags.Public | BindingFlags.Instance,
	//                 null,
	//                 Parameters,
	//                 null);
	//         }
	//     }

	//     public static class Type
	//     {
	//         public static readonly MethodInfo GetTypeFromHandle = typeof(System.Type).GetMethod("GetTypeFromHandle");
	//         public static readonly MethodInfo op_Equality =
	//             typeof(System.Type).GetMethod("op_Equality", BindingFlags.Static | BindingFlags.Public);
	//     }

	//     public static class StructBase
	//     {
	//         public static readonly MethodInfo Load =
	//             typeof(Il2CppToolkit.Runtime.StructBase).GetMethod("Load", BindingFlags.NonPublic | BindingFlags.Instance);

	//         public static class Ctor
	//         {
	//             public static readonly System.Type[] Parameters = { typeof(IMemorySource), typeof(ulong) };
	//             public static readonly ConstructorInfo ConstructorInfo = typeof(Il2CppToolkit.Runtime.StructBase).GetConstructor(
	//                 BindingFlags.NonPublic | BindingFlags.Instance,
	//                 null,
	//                 Parameters,
	//                 null
	//                 );
	//         }
	//     }

	//     public static class StaticInstance
	//     {
	//         public static class Ctor
	//         {
	//             public static System.Type[] Parameters = StructBase.Ctor.Parameters;
	//             public static readonly ConstructorInfo ConstructorInfo = typeof(Il2CppToolkit.Runtime.StaticInstance<>).GetConstructor(
	//                 BindingFlags.NonPublic | BindingFlags.Instance,
	//                 null,
	//                 Parameters,
	//                 null
	//                 );
	//         }
	//     }
	// }
}

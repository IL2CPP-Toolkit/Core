using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Il2CppToolkit.Model;
using Mono.Cecil;
using Vestris.ResourceLib;

namespace Il2CppToolkit.ReverseCompiler.Target.NetCore
{
	public class BuildModulePhase : CompilePhase
	{
		public override string Name => "Build Module";

		private IReadOnlyList<Func<TypeDescriptor, bool>> m_typeSelectors;
		private string m_asmName;
		private Version m_asmVersion;
		private ICompileContext m_context;
		private string m_outputPath;

		public override Task Initialize(ICompileContext context)
		{
			m_context = context;
			m_outputPath = m_context.Artifacts.Get(ArtifactSpecs.OutputPath);
			m_typeSelectors = m_context.Artifacts.Get(ArtifactSpecs.TypeSelectors);
			m_asmName = context.Artifacts.Get(ArtifactSpecs.AssemblyName);
			m_asmVersion = context.Artifacts.Get(ArtifactSpecs.AssemblyVersion);
			return Task.CompletedTask;
		}

		public override Task Execute()
		{
			IReadOnlyList<TypeDescriptor> includedDescriptors = FilterTypes(m_typeSelectors).ToList();

			AssemblyNameDefinition assemblyName = new(m_asmName, m_asmVersion);
			using AssemblyDefinition assemblyDefinition = AssemblyDefinition.CreateAssembly(
				assemblyName,
				m_context.Model.ModuleName,
				new ModuleParameters()
				{
					Kind = ModuleKind.Dll,
				});
			ModuleBuilder mb = new(m_context, assemblyDefinition);
			foreach (TypeDescriptor td in includedDescriptors)
				mb.IncludeTypeDefinition(td.TypeDef);

			mb.Build();

			Write(assemblyDefinition);

			return Task.CompletedTask;
		}

		private void Write(AssemblyDefinition assemblyDefinition)
		{
			string outputFile = m_outputPath;
			if (Path.IsPathRooted(outputFile) && !Directory.Exists(Path.GetDirectoryName(outputFile)))
			{
				Directory.CreateDirectory(Path.GetDirectoryName(outputFile));
			}
			if (Path.GetExtension(outputFile) != ".dll")
			{
				outputFile = Path.Combine(m_outputPath, $"{m_context.Model.ModuleName}.dll");
			}

#if NET5_0_OR_GREATER
			var refsToRemove = assemblyDefinition.MainModule.AssemblyReferences
				.Where(asmRef => asmRef.Name == "mscorlib" || asmRef.Name == "System.Private.CoreLib").ToArray();
			foreach (var asmRef in refsToRemove)
				assemblyDefinition.MainModule.AssemblyReferences.Remove(asmRef);
#endif

			assemblyDefinition.Write(outputFile, new WriterParameters() { });
			// set file version
			VersionResource vi = new();
			{
				vi.FileVersion = m_asmVersion.ToString();
				vi.SaveTo(outputFile);
			}
		}

		private IEnumerable<TypeDescriptor> FilterTypes(IReadOnlyList<Func<TypeDescriptor, bool>> typeSelectors)
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
	}
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Il2CppToolkit.Model;
using Mono.Cecil;
using Vestris.ResourceLib;

namespace Il2CppToolkit.ReverseCompiler.Target.NetCore
{
	public class BuildModulePhase : CompilePhase
	{
		public override string Name => "Build Module";

		private IReadOnlyList<Func<TypeDescriptor, ArtifactSpecs.TypeSelectorResult>> m_typeSelectors;
		private string m_asmName;
		private Version m_asmVersion;
		private ICompileContext m_context;
		private string m_outputPath;
		private bool m_includeCompilerGeneratedTypes;

		public override Task Initialize(ICompileContext context)
		{
			m_context = context;
			m_outputPath = m_context.Artifacts.Get(ArtifactSpecs.OutputPath);
			m_typeSelectors = m_context.Artifacts.Get(ArtifactSpecs.TypeSelectors);
			m_asmName = context.Artifacts.Get(ArtifactSpecs.AssemblyName);
			m_asmVersion = context.Artifacts.Get(ArtifactSpecs.AssemblyVersion);
			m_includeCompilerGeneratedTypes = context.Artifacts.Get(ArtifactSpecs.IncludeCompilerGeneratedTypes);
			return Task.CompletedTask;
		}

		public override Task Execute()
		{
			OnProgressUpdated(0, 100, "Initializing");

			IReadOnlyDictionary<Il2CppTypeDefinition, ArtifactSpecs.TypeSelectorResult> includedDescriptors = FilterTypes(m_typeSelectors);

			AssemblyNameDefinition assemblyName = new(m_asmName, m_asmVersion);
			using AssemblyDefinition assemblyDefinition = AssemblyDefinition.CreateAssembly(
				assemblyName,
				m_context.Model.ModuleName,
				new ModuleParameters()
				{
					Kind = ModuleKind.Dll,
				});
			ModuleBuilder mb = new(m_context, assemblyDefinition, includedDescriptors, m_includeCompilerGeneratedTypes);
			mb.ProcessDescriptors();

			mb.ProgressUpdated += OnBuilderProgressUpdated;
			mb.Build();

			Write(assemblyDefinition);

			OnProgressUpdated(100, 100, "");

			return Task.CompletedTask;
		}

		private void OnBuilderProgressUpdated(object sender, ProgressUpdatedEventArgs e)
		{
			OnProgressUpdated((int)((double)e.Completed / e.Total * 100), 100, e.DisplayName);
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
			try
			{
				// set file version
				VersionResource vi = new();
				{
					vi.FileVersion = m_asmVersion.ToString();
					vi.SaveTo(outputFile);
				}
			}
			catch (Exception e)
			{
				OnProgressUpdated(0, 99, "Failed to set file version, retrying in 1s...");
				Thread.Sleep(1000);
				try
				{
					// retry set file version
					VersionResource vi = new();
					{
						vi.FileVersion = m_asmVersion.ToString();
						vi.SaveTo(outputFile);
					}
				}
				catch { }
			}
		}

		private IReadOnlyDictionary<Il2CppTypeDefinition, ArtifactSpecs.TypeSelectorResult> FilterTypes(IReadOnlyList<Func<TypeDescriptor, ArtifactSpecs.TypeSelectorResult>> typeSelectors)
		{
			return m_context.Model.TypeDescriptors.GroupBy(descriptor => descriptor.TypeDef, descriptor => typeSelectors.Select(selector => selector(descriptor)).Aggregate((a, b) => a | b))
				.ToDictionary(group => group.Key, group => group.Aggregate((a, b) => a | b));
		}
	}
}
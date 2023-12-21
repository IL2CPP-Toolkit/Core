using Il2CppToolkit.Model;
using Il2CppToolkit.ReverseCompiler;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Threading.Tasks;
using TypeLitePlus;
using System.Diagnostics.CodeAnalysis;

namespace Il2CppToolkit.Target.TSDef
{
	class EmitTypeDefinitionsPhase : CompilePhase
	{
		protected enum TypeDefinitionState
		{
			Created,
			Existing,
			Excluded
		}
		public override string Name => "Emit Typescript Definitions";

		private ICompileContext m_context;
		private string m_assemblyName;
		private string m_outputPath;
		private TypeDefinitionsBuilder m_typeDefinitionsBuilder;

		private Il2Cpp Il2Cpp => m_context.Model.Il2Cpp;
		private Metadata Metadata => m_context.Model.Metadata;

		public override Task Initialize(ICompileContext context)
		{
			m_context = context;
			m_assemblyName = context.Artifacts.Get(ArtifactSpecs.AssemblyName);
			m_outputPath = m_context.Artifacts.Get(ArtifactSpecs.OutputPath);
			m_typeDefinitionsBuilder = new(context);
			return Task.CompletedTask;
		}

		public override Task Execute()
		{
			OnProgressUpdated(0, 100, "Initializing");

			m_typeDefinitionsBuilder.ProcessDescriptors();

			m_typeDefinitionsBuilder.ProgressUpdated += OnBuilderProgressUpdated;
			m_typeDefinitionsBuilder.BuildDefinitionQueue();

			string outputFile = m_outputPath;
			if (Path.IsPathRooted(outputFile) && !Directory.Exists(Path.GetDirectoryName(outputFile)))
			{
				Directory.CreateDirectory(Path.GetDirectoryName(outputFile));
			}
			if (Path.GetExtension(outputFile) != ".ts")
			{
				outputFile = Path.Combine(m_outputPath, $"{m_assemblyName}.ts");
			}
			Directory.CreateDirectory(Path.GetDirectoryName(outputFile));
			File.WriteAllText(outputFile, m_typeDefinitionsBuilder.Generate());

			OnProgressUpdated(100, 100, "");

			return Task.CompletedTask;
		}

		private void OnBuilderProgressUpdated(object sender, ProgressUpdatedEventArgs e)
		{
			OnProgressUpdated(e.Completed, e.Total, e.DisplayName);
		}

	}
}

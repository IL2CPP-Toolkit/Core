using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Il2CppToolkit.Model;
using Il2CppToolkit.ReverseCompiler.Target;

namespace Il2CppToolkit.ReverseCompiler
{
	public class Compiler
	{
		private readonly CompileContext m_context;
		private readonly List<ICompilerTarget> m_targets = new();

		public ArtifactContainer Artifacts => m_context.Artifacts;
		public event EventHandler<ProgressUpdatedEventArgs> ProgressUpdated;

		public Compiler(TypeModel model, ICompilerLogger logger = null)
		{
			m_context = new(model, logger);
			m_context.ProgressUpdated += (_, e) => ProgressUpdated?.Invoke(this, e);
		}

		public void AddTarget(ICompilerTarget target)
		{
			m_targets.Add(target);
		}

		public void AddConfiguration(params IStateSpecificationValue[] configuration)
		{
			foreach (IStateSpecificationValue value in configuration)
			{
				m_context.Artifacts.Set(value.Specification, value.Value);
			}
		}

		public async Task Compile()
		{
			foreach (ICompilerTarget target in m_targets)
			{
				foreach (var param in target.Parameters)
				{
					if (param.Required && !m_context.Artifacts.Has(param.Specification))
					{
						CompilerError.MissingParameter.Raise($"Compiler target {target.Name} is missing required parameter {param.Specification.Name}");
					}
				}
				foreach (var phase in target.Phases)
				{
					m_context.AddPhase(phase);
				}
			}
			await m_context.Execute();
		}
	}
}

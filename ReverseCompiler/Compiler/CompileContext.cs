using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Il2CppToolkit.Model;

namespace Il2CppToolkit.ReverseCompiler
{
	public class CompileContext : ICompileContext
	{
		private readonly HashSet<CompilePhase> m_phases = new();
		private readonly Dictionary<CompilePhase, int> m_phaseProgress = new();
		private readonly ArtifactContainer m_artifacts = new();

		public ITypeModelMetadata Model { get; }
		public ArtifactContainer Artifacts => m_artifacts;
		public ICompilerLogger Logger { get; }
		public event EventHandler<ProgressUpdatedEventArgs> ProgressUpdated;

		public CompileContext(ITypeModelMetadata model, ICompilerLogger logger)
		{
			Model = model;
			Logger = logger;
		}

		public void AddPhase<T>(T compilePhase) where T : CompilePhase
		{
			m_phases.Add(compilePhase);
			m_phaseProgress.Add(compilePhase, 0);
			compilePhase.ProgressUpdated += (_, e) =>
			{
				m_phaseProgress[compilePhase] = (int)(((double)e.Completed / e.Total) * 100);
				int completed = m_phaseProgress.Values.Sum();
				int total = m_phaseProgress.Count * 100;
				ProgressUpdated?.Invoke(this, new() { Completed = completed, Total = total, DisplayName = e.DisplayName });
			};
		}

		public Task WaitForPhase<T>() where T : CompilePhase
		{
			BuildArtifactSpecification<object> GetPhaseSpec()
			{
				return m_phases.Single(phase => phase.GetType() == typeof(T)).PhaseSpec;
			}
			return Artifacts.GetAsync(GetPhaseSpec());
		}

		public IEnumerable<CompilePhase> GetPhases()
		{
			foreach (CompilePhase phase in m_phases)
			{
				yield return phase;
			}
		}

		public async Task Execute()
		{
			int completed = 0;
			int total = m_phases.Count * 100;
			await Task.WhenAll(m_phases.Select(async phase =>
			{
				Trace.WriteLine($"[{phase.Name}]:Initialize");
				await phase.Initialize(this);

				Trace.WriteLine($"[{phase.Name}]:Execute");
				await phase.Execute();

				Trace.WriteLine($"[{phase.Name}]:Finalize");
				await phase.Finalize();

				Artifacts.Set(phase.PhaseSpec, new object());

				Trace.WriteLine($"[{phase.Name}]:Completed");
			}).ToArray());
		}
	}
}

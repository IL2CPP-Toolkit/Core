using System;
using System.Threading.Tasks;
using Il2CppToolkit.Model;

namespace Il2CppToolkit.ReverseCompiler
{
	public abstract class CompilePhase
	{
		protected void OnProgressUpdated(int completed, int total, string? displayName = null)
		{
			ProgressUpdated?.Invoke(this, new() { Completed = completed, Total = total, DisplayName = displayName });
		}
		public event EventHandler<ProgressUpdatedEventArgs> ProgressUpdated;
		public BuildArtifactSpecification<object> PhaseSpec;
		public abstract string Name { get; }
		public abstract Task Initialize(ICompileContext context);
		public virtual Task Execute() { return Task.CompletedTask; }
		public virtual Task Finalize() { return Task.CompletedTask; }

		protected CompilePhase()
		{
			PhaseSpec = new(Name);
		}
	}
}

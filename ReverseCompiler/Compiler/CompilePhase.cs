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

		private int Completed = 0;
		private int Total = 0;
		private int UpdateCounter = 0;
		private string CurrentAction;

		protected void AddWork(int count = 1)
		{
			Total += count;
			OnWorkUpdated();
		}

		protected void CompleteWork(int count = 1)
		{
			Completed += count;
			OnWorkUpdated();
		}

		protected void SetAction(string actionName)
		{
			if (CurrentAction == actionName)
				return;
			CurrentAction = actionName;
			UpdateCounter = -1; // force update when action changes
			OnWorkUpdated();
		}

		protected void OnWorkUpdated()
		{
			if (++UpdateCounter % 50 != 0 || Total < 10)
				return;
			OnProgressUpdated(Completed, Math.Max(Total, 1), CurrentAction);
		}

		protected CompilePhase()
		{
			PhaseSpec = new(Name);
		}
	}
}

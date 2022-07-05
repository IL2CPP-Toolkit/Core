using System.Threading.Tasks;
using Il2CppToolkit.Model;

namespace Il2CppToolkit.ReverseCompiler
{
	public abstract class CompilePhase
	{
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

using System.Collections.Generic;
using System.Threading.Tasks;
using Il2CppToolkit.Model;

namespace Il2CppToolkit.ReverseCompiler
{
	public interface ICompileContext
	{
		ITypeModel Model { get; }
		ArtifactContainer Artifacts { get; }

		void AddPhase<T>(T compilePhase) where T : CompilePhase;
		Task WaitForPhase<T>() where T : CompilePhase;
		IEnumerable<CompilePhase> GetPhases();
		Task Execute();
	}
}

using System.Collections.Generic;
using System.Threading.Tasks;
using Il2CppToolkit.Model;

namespace Il2CppToolkit.ReverseCompiler
{
	public interface ICompileContext
	{
		ITypeModelMetadata Model { get; }
		ArtifactContainer Artifacts { get; }
		ICompilerLogger Logger { get; }

		void AddPhase<T>(T compilePhase) where T : CompilePhase;
		Task WaitForPhase<T>() where T : CompilePhase;
		IEnumerable<CompilePhase> GetPhases();
		Task Execute();
	}
}

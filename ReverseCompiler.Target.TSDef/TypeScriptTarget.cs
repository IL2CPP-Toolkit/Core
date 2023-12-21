using Il2CppToolkit.ReverseCompiler;
using Il2CppToolkit.ReverseCompiler.Target;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Il2CppToolkit.Target.TSDef
{
    public class TypeScriptTarget : ICompilerTarget
    {
        public string Name => "TypeScript";

        public IEnumerable<CompilerTargetParameter> Parameters { get; } = new List<CompilerTargetParameter>()
        {
            {new CompilerTargetParameter { Specification = ArtifactSpecs.OutputPath, Required = true}},
            {new CompilerTargetParameter { Specification = ArtifactSpecs.TypeSelectors, Required = true}}
        };

        public IEnumerable<CompilePhase> Phases { get; } = new List<CompilePhase>()
        {
            new EmitTypeDefinitionsPhase()
        };
    }
}

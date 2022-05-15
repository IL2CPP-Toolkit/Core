using System.Collections.Generic;

namespace Il2CppToolkit.ReverseCompiler.Target.NetCore
{
    public class NetCoreTarget : ICompilerTarget
    {
        public string Name => "NetCore";

        public IEnumerable<CompilerTargetParameter> Parameters { get; } = new List<CompilerTargetParameter>()
        {
            {new CompilerTargetParameter { Specification = ArtifactSpecs.AssemblyName, Required = true}},
            {new CompilerTargetParameter { Specification = ArtifactSpecs.OutputPath, Required = true}},
            {new CompilerTargetParameter { Specification = ArtifactSpecs.TypeSelectors, Required = true}}
        };

        public IEnumerable<CompilePhase> Phases { get; } = new List<CompilePhase>()
        {
            new SortDependenciesPhase(),
            new DefineTypesPhase(),
            // new BuildTypesPhase(),
            // new GenerateAssemblyPhase()
        };
    }
}
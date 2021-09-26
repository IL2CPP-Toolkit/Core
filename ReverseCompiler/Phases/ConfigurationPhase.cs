using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Il2CppToolkit.Model;

namespace Il2CppToolkit.ReverseCompiler
{
    public class ConfigurationPhase : CompilePhase
    {
        public override string Name => "Configuration";
        public readonly List<Func<TypeDescriptor, bool>> TypeSelectors;

        public ConfigurationPhase(List<Func<TypeDescriptor, bool>> typeSelectors)
        {
            TypeSelectors = typeSelectors;
        }

        public override Task Execute(CompileContext context)
        {
            context.AddArtifact("TypeSelectors", TypeSelectors);
            return Task.CompletedTask;
        }
    }
}

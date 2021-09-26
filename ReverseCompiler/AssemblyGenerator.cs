using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Il2CppToolkit.Model;

namespace Il2CppToolkit.ReverseCompiler
{
    public partial class AssemblyGenerator
    {
        private CompileContext m_context;

        public List<Func<TypeDescriptor, bool>> TypeSelectors = new();
        public string AssemblyName { get; set; }

        public AssemblyGenerator(TypeModel model)
        {
            m_context = new(model);
            m_context.AddPhase(new SortDependenciesPhase());
            m_context.AddPhase(new BuildTypesPhase());
        }

        public async Task Execute()
        {
            m_context.Artifacts.Set(ArtifactSpecs.TypeSelectors, TypeSelectors);
            m_context.Artifacts.Set(ArtifactSpecs.AssemblyName, AssemblyName);

            await Task.WhenAll(m_context.GetPhases().Select(async phase =>
            {
                Trace.WriteLine($"[{phase.Name}]:Initialize");
                await phase.Initialize(m_context);

                Trace.WriteLine($"[{phase.Name}]:Execute");
                await phase.Execute();

                Trace.WriteLine($"[{phase.Name}]:Finalize");
                await phase.Finalize();

                Trace.WriteLine($"[{phase.Name}]:Completed");
            }).ToArray());
        }
    }
}

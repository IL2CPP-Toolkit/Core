using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Il2CppToolkit.Common.Errors;
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

            IEnumerable<CompilePhase> compilePhases = GetType().Assembly
                .GetTypes()
                .Where(type => !type.IsAbstract && type.IsAssignableTo(typeof(CompilePhase)))
                .Select(type => (CompilePhase)Activator.CreateInstance(type));

            foreach (var phase in compilePhases)
            {
                m_context.AddPhase(phase);
            }
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

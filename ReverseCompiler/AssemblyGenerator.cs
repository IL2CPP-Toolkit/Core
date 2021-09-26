using System;
using System.Collections.Generic;
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
        public string OutputPath { get; set; }

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

        public async Task GenerateAssembly()
        {
            m_context.Artifacts.Set(ArtifactSpecs.TypeSelectors, TypeSelectors);
            m_context.Artifacts.Set(ArtifactSpecs.AssemblyName, AssemblyName);
            m_context.Artifacts.Set(ArtifactSpecs.OutputPath, OutputPath);
            await m_context.Execute();
        }
    }
}

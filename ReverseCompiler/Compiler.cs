using System.Collections.Generic;
using System.Threading.Tasks;
using Il2CppToolkit.Model;
using Il2CppToolkit.ReverseCompiler.Target;

namespace Il2CppToolkit.ReverseCompiler
{
    public class Compiler
    {
        private CompileContext m_context;
        private List<ICompilerTarget> m_targets = new();

        public ArtifactContainer Artifacts => m_context.Artifacts;

        public Compiler(TypeModel model)
        {
            m_context = new(model);
        }

        public void AddTarget(ICompilerTarget target)
        {
            m_targets.Add(target);
        }

        public void AddConfiguration(params IStateSpecificationValue[] configuration)
        {
            foreach (IStateSpecificationValue value in configuration)
            {
                m_context.Artifacts.Set(value.Specification, value.Value);
            }
        }

        public async Task Compile()
        {
            foreach (ICompilerTarget target in m_targets)
            {
                foreach (var param in target.Parameters)
                {
                    if (param.Required && !m_context.Artifacts.Has(param.Specification))
                    {
                        CompilerError.MissingParameter.Raise($"Compiler target {target.Name} is missing required parameter {param.Specification.Name}");
                    }
                }
                foreach (var phase in target.Phases)
                {
                    m_context.AddPhase(phase);
                }
            }
            await m_context.Execute();
        }
    }
}

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

        public AssemblyGenerator(TypeModel model)
        {
            m_context = new(this, model);
            m_context.AddPhase(new TypeGenerationPhase());
            m_context.AddPhase(new ConfigurationPhase(TypeSelectors));
        }

        public async Task Execute()
        {
            await Task.WhenAll(m_context.GetPhases().Select(async phase =>
            {
                Trace.WriteLine($"[{phase.Name}]:Prologue");
                await phase.Prologue(m_context);

                Trace.WriteLine($"[{phase.Name}]:Execute");
                await phase.Execute(m_context);

                Trace.WriteLine($"[{phase.Name}]:Epilogue");
                await phase.Epilogue(m_context);

                Trace.WriteLine($"[{phase.Name}]:Completed");
            }).ToArray());
        }
    }
}

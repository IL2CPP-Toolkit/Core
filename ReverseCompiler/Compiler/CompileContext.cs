using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Il2CppToolkit.Model;

namespace Il2CppToolkit.ReverseCompiler
{
    public class CompileContext
    {
        private HashSet<CompilePhase> m_phases = new();
        private ArtifactContainer m_artifacts = new();

        public TypeModel Model { get; }
        public ArtifactContainer Artifacts => m_artifacts;

        public CompileContext(TypeModel model)
        {
            Model = model;
        }

        public void AddPhase<T>(T compilePhase) where T : CompilePhase
        {
            m_phases.Add(compilePhase);
        }

        public IEnumerable<CompilePhase> GetPhases()
        {
            foreach (CompilePhase phase in m_phases)
            {
                yield return phase;
            }
        }
    }
}

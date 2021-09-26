using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Il2CppToolkit.Model;

namespace Il2CppToolkit.ReverseCompiler
{
    public class CompileContext
    {
        private Queue<CompilePhase> m_queue = new();
        private List<CompilePhase> m_phases = new();
        private ArtifactSet<CompileArtifact, object> m_artifacts = new();

        public AssemblyGenerator AssemblyGenerator { get; }
        public TypeModel Model { get; }

        public CompileContext(AssemblyGenerator asmGen, TypeModel model)
        {
            AssemblyGenerator = asmGen;
            Model = model;
        }

        public void AddArtifact<T>(string name, T value)
        {
            m_artifacts.Add(new CompileArtifact(name, typeof(T)), value);
        }

        public Task<T> GetArtifact<T>(string name)
        {
            return m_artifacts.Get<T>(new CompileArtifact(name, typeof(T)));
        }

        public void AddPhase<T>(T compilePhase) where T : CompilePhase
        {
            m_queue.Enqueue(compilePhase);
        }

        public IEnumerable<CompilePhase> GetPhases()
        {
            foreach (CompilePhase phase in m_queue)
            {
                yield return phase;
                m_phases.Add(phase);
            }
        }

        public T GetPhaseOutput<T>() where T : CompilePhase
        {
            return (T)m_phases.FirstOrDefault(phase => phase.GetType().IsAssignableTo(typeof(T)));
        }
    }
}

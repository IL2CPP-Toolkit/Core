using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using Il2CppToolkit.Common.Errors;
using Il2CppToolkit.Model;
using Il2CppToolkit.Runtime;
using Il2CppToolkit.Runtime.Types.Reflection;

namespace Il2CppToolkit.ReverseCompiler.Target.NetCore
{
    public class BuildTypesPhase : CompilePhase
    {
        public override string Name => "Build Types";

        private CompileContext m_context;
        private IReadOnlyList<TypeDescriptor> m_typeDescriptors;
        private IReadOnlyDictionary<TypeDescriptor, IGeneratedType> m_generatedTypes;
        private BuildTypeResolver m_typeResolver;
        private ConstructorCache m_ctorCache;

        public override async Task Initialize(CompileContext context)
        {
            m_context = context;
            m_typeDescriptors = await context.Artifacts.GetAsync(NetCoreArtifactSpecs.SortedTypeDescriptors);
            m_generatedTypes = await m_context.Artifacts.GetAsync(NetCoreArtifactSpecs.GeneratedTypes);

            m_typeResolver = new(context, m_generatedTypes);
            m_ctorCache = new();
        }

        public override Task Execute()
        {
            ProcessTypes();
            return Task.CompletedTask;
        }

        public override Task Finalize()
        {
            return Task.CompletedTask;
        }

        private void ProcessTypes()
        {
            List<TypeDescriptor> sortedTypes = new();
            HashSet<TypeDescriptor> visited = new();
            void ProcessNext(TypeDescriptor next)
            {
                if (visited.Contains(next)) return;
                if (next.Base != null)
                {
                    foreach (var dep in m_typeResolver.GetGeneratedDependents(next.Base))
                    {
                        ProcessNext(dep);
                        if (!visited.Contains(dep))
                        {
                            sortedTypes.Add(dep);
                            visited.Add(dep);
                        }
                    }
                }
                if (!visited.Contains(next))
                {
                    sortedTypes.Add(next);
                    visited.Add(next);
                }
            }
            foreach (var next in m_generatedTypes.Keys)
            {
                ProcessNext(next);
            }
            foreach (TypeDescriptor td in sortedTypes)
            {
                IGeneratedType type = m_generatedTypes[td];
                type.Build(m_typeResolver, m_ctorCache);
            }
        }
    }
}

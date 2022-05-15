using System.Collections.Generic;
using Il2CppToolkit.Model;
using Mono.Cecil;

namespace Il2CppToolkit.ReverseCompiler.Target.NetCore
{
    public static class NetCoreArtifactSpecs
    {
        public static BuildArtifactSpecification<IReadOnlyList<TypeDescriptor>> SortedTypeDescriptors = new("SortedDescriptors");
        public static BuildArtifactSpecification<IReadOnlyDictionary<TypeDescriptor, IGeneratedType>> GeneratedTypes = new("GeneratedTypes");
        public static BuildArtifactSpecification<ModuleDefinition> GeneratedModule = new("GeneratedModule");
    }
}
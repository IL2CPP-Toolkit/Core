using System.Collections.Generic;
using System.Reflection.Emit;
using Il2CppToolkit.Model;

namespace Il2CppToolkit.ReverseCompiler.Target.NetCore
{
    public static class NetCoreArtifactSpecs
    {
        public static BuildArtifactSpecification<IReadOnlyList<TypeDescriptor>> SortedTypeDescriptors = new("SortedDescriptors");
        public static BuildArtifactSpecification<IReadOnlyDictionary<TypeDescriptor, IGeneratedType>> GeneratedTypes = new("GeneratedTypes");
        public static BuildArtifactSpecification<ModuleBuilder> GeneratedModule = new("GeneratedModule");
    }
}
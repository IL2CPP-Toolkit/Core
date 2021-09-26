using System;
using System.Collections.Generic;
using Il2CppToolkit.Model;

namespace Il2CppToolkit.ReverseCompiler
{
    public static class ArtifactSpecs
    {
        public static SynchronousVariableSpecification<IReadOnlyList<Func<TypeDescriptor, bool>>> TypeSelectors = new("TypeSelectors");
        public static SynchronousVariableSpecification<string> AssemblyName = new("AssemblyName");

        public static BuildArtifactSpecification<IReadOnlyList<TypeDescriptor>> SortedTypeDescriptors = new("SortedDescriptors");
    }
}
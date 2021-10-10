using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using Il2CppToolkit.Model;

namespace Il2CppToolkit.ReverseCompiler
{
    public static class ArtifactSpecs
    {
        public static SynchronousVariableSpecification<IReadOnlyList<Func<TypeDescriptor, bool>>> TypeSelectors = new("TypeSelectors");
        public static SynchronousVariableSpecification<string> AssemblyName = new("AssemblyName");
        public static SynchronousVariableSpecification<string> OutputPath = new("OutputPath");
    }
}
using System;

namespace Il2CppToolkit.ReverseCompiler
{
    internal struct CompileArtifact
    {
        public string Name;
        public Type Type;

        public CompileArtifact(string name, Type type)
        {
            Name = name;
            Type = type;
        }

        public override bool Equals(object obj)
        {
            if (obj is CompileArtifact other)
            {
                return other.Name == Name && other.Type.IsAssignableTo(Type);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name, Type);
        }
    }

}
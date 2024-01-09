using System.Collections.Generic;

namespace Il2CppToolkit.Model
{
    public class MethodDescriptor
    {
        public MethodDescriptor(string name)
        {
            Name = name;
        }

        public string DisambiguatedName;
        public readonly string Name;
        public readonly List<ITypeReference> DeclaringTypeArgs = new();
    }
}

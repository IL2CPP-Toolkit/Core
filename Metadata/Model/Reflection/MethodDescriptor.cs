using System.Collections.Generic;

namespace Il2CppToolkit.Model
{
    public class MethodDescriptor
    {
        public MethodDescriptor(string name, ulong address)
        {
            Name = name;
            Address = address;
        }

        public string DisambiguatedName;
        public readonly string Name;
        public readonly ulong Address;
        public readonly List<ITypeReference> DeclaringTypeArgs = new();
    }
}

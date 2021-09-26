using System.Collections.Generic;

namespace Il2CppToolkit.Model
{
    public partial class TypeModel
    {
        public IReadOnlyList<TypeDescriptor> TypeDescriptors => m_typeDescriptors;
    }
}
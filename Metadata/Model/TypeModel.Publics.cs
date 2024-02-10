using System.Collections.Generic;

namespace Il2CppToolkit.Model
{
	public partial class TypeModel
	{
		// TODO: Encapsulate functionality
		public Il2Cpp Il2Cpp => m_loader.Il2Cpp;
		public Metadata Metadata => m_loader.Metadata;

		public string ModuleName => m_loader.ModuleName;
		public IReadOnlyList<TypeDescriptor> TypeDescriptors => m_typeDescriptors;
	}
}
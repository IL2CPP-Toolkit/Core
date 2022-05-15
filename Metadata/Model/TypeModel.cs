using System.Collections.Generic;

namespace Il2CppToolkit.Model
{
	public partial class TypeModel : ITypeModel
	{
		private readonly Loader m_loader;
		private ulong[] m_fieldOffsets;

		public TypeModel(Loader loader)
		{
			m_loader = loader;
			Load();
		}

		private void Load()
		{
			LoadFieldOffsets();
			IndexTypeDescriptors();
		}

		private void LoadFieldOffsets()
		{
			if (m_loader.Il2Cpp.FieldOffsetsArePointers)
			{
				m_fieldOffsets = new ulong[m_loader.Metadata.fieldDefs.Length];
				SortedDictionary<int, ulong> sortedOffsets = new();
				foreach ((int typeIdx, Il2CppTypeDefinition def) in m_loader.Metadata.typeDefs.WithIndexes())
				{
					var typeFieldsOffsetPtr = m_loader.Il2Cpp.FieldOffsets[typeIdx];
					if (typeFieldsOffsetPtr == 0)
						continue;

					ulong fieldStartPtr = m_loader.Il2Cpp.MapVATR(typeFieldsOffsetPtr);
					if (fieldStartPtr == 0)
						continue;

					m_loader.Il2Cpp.Position = fieldStartPtr;
					for (var fieldIdx = 0; fieldIdx < def.field_count; ++fieldIdx)
						m_fieldOffsets[def.fieldStart + fieldIdx] = m_loader.Il2Cpp.ReadUInt32();
				}
			}
			else
			{
				m_fieldOffsets = m_loader.Il2Cpp.FieldOffsets;
			}
		}
	}
}
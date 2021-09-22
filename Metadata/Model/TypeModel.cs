using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Il2CppToolkit.Common;

namespace Il2CppToolkit.Model
{
	public partial class TypeModel
	{
		private Loader Loader;
		private ulong[] FieldOffsets;

		public TypeModel(Loader loader)
		{
			Loader = loader;
			Load();
		}

		private void Load()
		{
			LoadFieldOffsets();
			IndexTypeDescriptors();
		}

		private void LoadFieldOffsets()
		{
			if (Loader.Il2Cpp.FieldOffsetsArePointers)
			{
				FieldOffsets = new ulong[Loader.Metadata.fieldDefs.Length];
				SortedDictionary<int, ulong> sortedOffsets = new();
				foreach ((int typeIdx, Il2CppTypeDefinition def) in Loader.Metadata.typeDefs.WithIndexes())
				{
					var typeFieldsOffsetPtr = Loader.Il2Cpp.FieldOffsets[typeIdx];
					if (typeFieldsOffsetPtr == 0)
						continue;

					ulong fieldStartPtr = Loader.Il2Cpp.MapVATR(typeFieldsOffsetPtr);
					if (fieldStartPtr == 0)
						continue;

					for (var fieldIdx = 0; fieldIdx < def.field_count; ++fieldIdx)
						FieldOffsets[def.fieldStart + fieldIdx] = Loader.Il2Cpp.ReadUInt32();
				}
			}
			else
			{
				FieldOffsets = Loader.Il2Cpp.FieldOffsets;
			}
		}
	}
}
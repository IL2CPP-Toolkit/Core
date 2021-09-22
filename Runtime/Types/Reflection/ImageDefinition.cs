using System;
using IL2CS.Core;
using IL2CS.Runtime.Types.corelib;

namespace IL2CS.Runtime.Types.Reflection
{
	// https://github.com/Orkanelf/MRRemoteGuiding/blob/fdb2d48b9e14e25505a00fa3dfa611be36f4bae5/AR-Application-Scherf_neu/Builds/Il2CppOutputProject/IL2CPP/libil2cpp/il2cpp-class-internals.h
	public class ImageDefinition
	{
		[Offset(0x0)]
#pragma warning disable 649
		private Native__LPSTR m_name;
#pragma warning restore 649
		public string Name { get { return m_name.Value; } }

		[Offset(0x8)]
#pragma warning disable 649
		private Native__LPSTR m_nameNoExtension;
#pragma warning restore 649
		public string NameNoExtension { get { return m_nameNoExtension.Value; } }

		// 0x10 = AssemblyDefinition

		[field: Offset(0x18)] public int TypeStart { get; }
	}
}

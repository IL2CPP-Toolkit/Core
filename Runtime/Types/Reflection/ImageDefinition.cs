using System;
using Il2CppToolkit.Runtime.Types.corelib;

namespace Il2CppToolkit.Runtime.Types.Reflection
{
    // https://github.com/Orkanelf/MRRemoteGuiding/blob/fdb2d48b9e14e25505a00fa3dfa611be36f4bae5/AR-Application-Scherf_neu/Builds/Il2CppOutputProject/IL2CPP/libil2cpp/il2cpp-class-internals.h
    [Size(0x28)]
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

        [field: Offset(0x18)] public UInt32 TypeStart { get; }
        [field: Offset(0x40)] public UInt32 Token { get; }
    }
}

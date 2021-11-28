using System;
using Il2CppToolkit.Runtime.Types.corelib;

namespace Il2CppToolkit.Runtime.Types.Reflection
{
    // https://github.com/ascv0228/il2cpp/blob/48fd00e4e7a2a1b4332b8c2ea5c242b46f3209a4/il2cpp-types.h
    [Size(0x20)]
    public struct FieldDefinition
    {
        [Offset(0x0)]
#pragma warning disable 649
        private Native__LPSTR m_name;
#pragma warning restore 649
        public string Name { get { return m_name.Value; } }

        [field: Offset(0x18)] public int Offset { get; }
    }
}

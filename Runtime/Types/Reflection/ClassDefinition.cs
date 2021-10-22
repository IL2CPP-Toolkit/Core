using System.Diagnostics;
using Il2CppToolkit.Runtime.Types.corelib;

namespace Il2CppToolkit.Runtime.Types.Reflection
{
    // [Size(4376)]
    [Size(0x120)]
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class ClassDefinition
    {
        private string DebuggerDisplay
        {
            get
            {
                if (string.IsNullOrEmpty(Namespace) && string.IsNullOrEmpty(Name))
                {
                    return "(none)";
                }
                return FullName;
            }
        }

        public string FullName
        {
            get
            {
                if (string.IsNullOrEmpty(Namespace))
                {
                    return Name;
                }
                else
                {
                    return $"{Namespace}.{Name}";
                }
            }
        }

        [field: Offset(0)] public ImageDefinition Image { get; }

        [Offset(16)]
#pragma warning disable 649
        private Native__LPSTR m_name;
#pragma warning restore 649
        public string Name { get { return m_name.Value; } }


        [Offset(24)]
#pragma warning disable 649
        private Native__LPSTR m_namespace;
#pragma warning restore 649
        public string Namespace { get { return m_namespace.Value; } }

#pragma warning restore 649
        [field: Offset(184)] public UnknownClass StaticFields { get; }

        [field: Offset(0x118)] public uint Token { get; }
    }
}

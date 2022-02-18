using System;
using System.Diagnostics;
using Il2CppToolkit.Runtime.Types.corelib;
using Il2CppToolkit.Runtime.Types.corelib.Collections.Generic;

#pragma warning disable 649
namespace Il2CppToolkit.Runtime.Types.Reflection
{
    // [Size(4376)]
    [Size(0x128)]
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class ClassDefinition : StructBase
    {
        public ClassDefinition(IMemorySource source, ulong address) : base(source, address)
        {
        }

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
                if (Parent == null)
                {
                    return GetLocalTypeName(Namespace, Name);
                }
                else
                {
                    return $"{Parent.FullName}+{Name}";
                }
            }
        }

        private static string GetLocalTypeName(string ns, string name)
        {
            if (string.IsNullOrEmpty(ns))
            {
                return name;
            }
            else
            {
                return $"{ns}.{name}";
            }
        }

        [Offset(0)]
        private ImageDefinition _image;
        public ImageDefinition Image
        {
            get
            {
                Load();
                return _image;
            }
        }

        [Offset(0x10)]
        private Native__LPSTR m_name;
        public string Name
        {
            get
            {
                Load();
                return m_name.Value;
            }
        }


        [Offset(0x18)]
        private Native__LPSTR m_namespace;
        public string Namespace
        {
            get
            {
                Load();
                return m_namespace.Value;
            }
        }

        [Offset(0x50)]
        private ClassDefinition m_parent;
        public ClassDefinition Parent
        {
            get
            {
                Load();
                return m_parent;
            }
        }

        [Offset(0x58)]
        private ClassDefinition m_base;
        public ClassDefinition Base
        {
            get
            {
                Load();
                return m_base;
            }
        }

        [Ignore]
        private FieldDefinition[] _fields;
        public FieldDefinition[] GetFields()
        {
            if (_fields == null)
            {
                ulong firstElementPtr = this.MemorySource.ReadPointer(this.Address + 0x80);
                if (firstElementPtr == 0)
                {
                    return _fields = Array.Empty<FieldDefinition>();
                }

                Load();
                _fields = new Native__RawArray<FieldDefinition>(this.MemorySource, firstElementPtr, FieldCount).Array;
            }
            return _fields;
        }

        [Offset(0xB8)]
        private UnknownClass _staticFields;
        public UnknownClass StaticFields
        {
            get
            {
                Load();
                return _staticFields;
            }
        }

        [Offset(0x114)]
        private uint _token;
        public uint Token
        {
            get
            {
                Load();
                return _token;
            }
        }

        [Offset(0x120)]
        private UInt16 _fieldCount;
        public UInt16 FieldCount
        {
            get
            {
                Load();
                return _fieldCount;
            }
        }
    }
}
#pragma warning restore 649

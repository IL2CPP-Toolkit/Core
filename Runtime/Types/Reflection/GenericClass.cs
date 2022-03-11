using System;
using System.Diagnostics;
using Il2CppToolkit.Runtime.Types.corelib;
using Il2CppToolkit.Runtime.Types.corelib.Collections.Generic;

#pragma warning disable 649
namespace Il2CppToolkit.Runtime.Types.Reflection
{
    public class Il2CppTypeDefinition : StructBase
    {
        public Il2CppTypeDefinition(IMemorySource source, ulong address)
            : base(source, address)
        {
        }

        [Offset(0x0)]
        public Int32 _nameIndex;
        public Int32 NameIndex
        {
            get
            {
                Load();
                return _nameIndex;
            }
        }

        [Offset(0x4)]
        public Int32 _namespaceIndex;
        public Int32 NamespaceIndex
        {
            get
            {
                Load();
                return _namespaceIndex;
            }
        }

        [Offset(0x54)]
        public UInt32 _token;
        public UInt32 Token
        {
            get
            {
                Load();
                return _token;
            }
        }
    }

    public class Il2CppType : StructBase
    {
        public Il2CppType(IMemorySource source, ulong address)
            : base(source, address)
        {
        }

        [Offset(0x0)]
        private Il2CppTypeDefinition m_typeHandle;
        public Il2CppTypeDefinition TypeHandle
        {
            get
            {
                Load();
                return m_typeHandle;
            }
        }
    }

    [Size(0x10)]
    public class Il2CppGenericInst : StructBase
    {
        public Il2CppGenericInst(IMemorySource source, ulong address)
            : base(source, address)
        {
        }

        [Offset(0x0)]
        private UInt32 m_count;
        public UInt32 Count
        {
            get
            {
                Load();
                return m_count;
            }
        }

        [Ignore]
        private Il2CppType[] m_types;
        public Il2CppType[] Types
        {
            get
            {
                {
                    if (m_types == null)
                    {
                        ulong firstElementPtr = this.MemorySource.ReadPointer(this.Address + 0x8);
                        if (firstElementPtr == 0)
                        {
                            return m_types = Array.Empty<Il2CppType>();
                        }

                        Load();
                        m_types = new Native__RawArray<Il2CppType>(this.MemorySource, firstElementPtr, Count).Array;
                    }
                    return m_types;
                }
            }
        }
    }

    [Size(0x10)]
    public struct Il2CppGenericContext
    {
        [Offset(0x0)]
        public Il2CppGenericInst ClassInst;
        [Offset(0x8)]
        public Il2CppGenericInst MethodInst;
    }

    [Size(0x10)]
    public class GenericClass : StructBase
    {
        public GenericClass(IMemorySource source, ulong address)
            : base(source, address)
        {
        }

        [Offset(0x8)]
        private Il2CppGenericContext m_genericContext;
        public Il2CppGenericContext GenericContext
        {
            get
            {
                Load();
                return m_genericContext;
            }
        }
    }
}
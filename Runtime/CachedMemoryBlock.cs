using System;
using System.Collections.Generic;

namespace Il2CppToolkit.Runtime
{
    public class CachedMemoryBlock : IMemorySource
    {
        public IMemorySource Parent { get; }
        private readonly ReadOnlyMemory<byte> Data;
        private readonly ulong Address;
        private readonly ulong Size; // in bytes

        public CachedMemoryBlock(IMemorySource parent, ulong address, byte[] data)
        {
            Parent = parent;
            Data = new ReadOnlyMemory<byte>(data);
            Address = address;
            Size = (ulong)data.Length;
        }

        public Il2CsRuntimeContext ParentContext
        {
            get
            {
                IMemorySource parent = Parent;
                while (!(parent is Il2CsRuntimeContext))
                {
                    parent = parent.Parent;
                }
                return parent as Il2CsRuntimeContext;
            }
        }

        public bool Contains(ulong address, ulong size)
        {
            if (address >= Address + Size)
            {
                return false;
            }
            if (address + size > Address + Size)
            {
                return false;
            }
            if (address < Address)
            {
                return false;
            }
            return true;
        }

        public ReadOnlyMemory<byte> ReadMemory(ulong address, ulong size)
        {
            if (!Contains(address, size))
            {
                return Parent.ReadMemory(address, size);
            }
            return Data.Slice((int)(address - Address), (int)size);
        }
    }
}
using System;
using System.Collections;
using System.Collections.Generic;

namespace Il2CppToolkit.Runtime.Types.corelib.Collections.Generic
{
    [TypeMapping(typeof(HashSet<>))]
    public class Native__HashSet<T> : StructBase, IReadOnlyCollection<T>, INullConstructable
    {
        public Native__HashSet(IMemorySource source, ulong address)
            : base(source, address)
        {
        }

        [Size(0x10)]
        public struct Entry
        {
            [Offset(0x00)]
            public UInt32 HashCode;
            [Offset(0x04)]
            public UInt32 Next;
            [Offset(0x08)]
            public T Value;
        }

        private readonly HashSet<T> m_set = new();

        private void ReadFields(IMemorySource source, ulong address)
        {
            if (Address == 0)
                return;

            uint count = source.ReadValue<uint>(address + 0x20);
            Native__Array<Entry> entries = new(source, source.ReadPointer(address + 0x18), count);
            entries.Load();
            foreach (Entry entry in entries)
            {
                m_set.Add(entry.Value);
            }
        }

        public HashSet<T> UnderlyingHashSet
        {
            get
            {
                Load();
                return m_set;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return UnderlyingHashSet.GetEnumerator();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return UnderlyingHashSet.GetEnumerator();
        }

        public int Count
        {
            get { return UnderlyingHashSet.Count; }
        }
    }
}

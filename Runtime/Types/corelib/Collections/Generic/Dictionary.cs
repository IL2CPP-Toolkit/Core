using System;
using System.Collections;
using System.Collections.Generic;

namespace Il2CppToolkit.Runtime.Types.corelib.Collections.Generic
{
    [TypeMapping(typeof(Dictionary<,>))]
    public class Native__Dictionary<TKey, TValue> : StructBase, IReadOnlyDictionary<TKey, TValue>
    {
        public Native__Dictionary(IMemorySource source, ulong address)
            : base(source, address)
        {
        }

        [Size(0x18)]
        public struct Entry
        {
            [Offset(0x00)]
            public UInt32 HashCode;
            [Offset(0x04)]
            public UInt32 Next;
            [Offset(0x08)]
            public TKey Key;
            [Offset(0x10)]
            public TValue Value;
        }
        [Size(0x10)]
        public struct EntryNarrow
        {
            [Offset(0x00)]
            public UInt32 HashCode;
            [Offset(0x04)]
            public UInt32 Next;
            [Offset(0x08)]
            public TKey Key;
            [Offset(0x0C)]
            public TValue Value;
        }
        private readonly Dictionary<TKey, TValue> m_dict = new();

        private void ReadFields(IMemorySource source, ulong address)
        {
            uint count = source.ReadValue<uint>(address + 0x20);
            if (Il2CsRuntimeContext.GetTypeSize(typeof(TValue)) <= 4 && Il2CsRuntimeContext.GetTypeSize(typeof(TKey)) <= 4)
            {
                Native__Array<EntryNarrow> entries = new(source, source.ReadPointer(address + 0x18), count);
                entries.Load();
                foreach (EntryNarrow entry in entries)
                {
                    m_dict.Add(entry.Key, entry.Value);
                }
            }
            else
            {
                Native__Array<Entry> entries = new(source, source.ReadPointer(address + 0x18), count);
                entries.Load();
                foreach (Entry entry in entries)
                {
                    //TODO: Log something?
                    m_dict.TryAdd(entry.Key, entry.Value);
                }
            }
        }

        public Dictionary<TKey, TValue> UnderlyingDictionary
        {
            get
            {
                Load();
                return m_dict;
            }
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return UnderlyingDictionary.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return UnderlyingDictionary.GetEnumerator();
        }

        public int Count
        {
            get { return UnderlyingDictionary.Count; }
        }

        public bool ContainsKey(TKey key)
        {
            return UnderlyingDictionary.ContainsKey(key);
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return UnderlyingDictionary.TryGetValue(key, out value);
        }

        public TValue this[TKey key]
        {
            get { return UnderlyingDictionary[key]; }
        }

        public IEnumerable<TKey> Keys
        {
            get { return UnderlyingDictionary.Keys; }
        }

        public IEnumerable<TValue> Values
        {
            get { return UnderlyingDictionary.Values; }
        }
    }
}

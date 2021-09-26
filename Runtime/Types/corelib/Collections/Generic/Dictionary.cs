using System;
using System.Collections;
using System.Collections.Generic;

namespace Il2CppToolkit.Runtime.Types.corelib.Collections.Generic
{
    [TypeMapping(typeof(Dictionary<,>))]
    public class Native__Dictionary<TKey, TValue> : StructBase, IReadOnlyDictionary<TKey, TValue>
    {
        public Native__Dictionary(Il2CsRuntimeContext context, ulong address)
            : base(context, address)
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
            // TODO: Offset should shift based on sizeof(TKey) [ requires size information from IL2CppDumper, which is not currently public :( ]
            [Offset(0x10)]
            public TValue Value;
        }
        private readonly Dictionary<TKey, TValue> m_dict = new();

        private void ReadFields(Il2CsRuntimeContext context, ulong address)
        {
            uint count = context.ReadValue<uint>(address + 0x20);
            Native__Array<Entry> entries = new(context, context.ReadPointer(address + 0x18), count);
            entries.Load();
            foreach (Entry entry in entries)
            {
                m_dict.Add(entry.Key, entry.Value);
            }
        }

        private Dictionary<TKey, TValue> Dict
        {
            get
            {
                Load();
                return m_dict;
            }
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return Dict.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Dict.GetEnumerator();
        }

        public int Count
        {
            get { return Dict.Count; }
        }

        public bool ContainsKey(TKey key)
        {
            return Dict.ContainsKey(key);
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return Dict.TryGetValue(key, out value);
        }

        public TValue this[TKey key]
        {
            get { return Dict[key]; }
        }

        public IEnumerable<TKey> Keys
        {
            get { return Dict.Keys; }
        }

        public IEnumerable<TValue> Values
        {
            get { return Dict.Values; }
        }
    }
}

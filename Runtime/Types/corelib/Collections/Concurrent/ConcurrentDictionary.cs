using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Il2CppToolkit.Runtime.Types.corelib.Collections.Generic;

namespace Il2CppToolkit.Runtime.Types.corelib.Collections.Concurrent
{
    [TypeMapping(typeof(ConcurrentDictionary<,>))]
    public class Native__ConcurrentDictionary<TKey, TValue> : StructBase, IReadOnlyDictionary<TKey, TValue>
    {
        public Native__ConcurrentDictionary(Il2CsRuntimeContext context, ulong address)
            : base(context, address)
        {
        }

        [Size(0x18)]
        public class Node
        {
            [Offset(0x10)]
            public TKey Key;
            [Offset(0x18)]
            public TValue Value;
            [Offset(0x20)]
            public Node Next;
        }

        public class Table
        {
            [Offset(0x10)]
            public Native__Array<Node> Buckets;
        }

        [Offset(0x10)]
#pragma warning disable 649
        private Table m_table;
#pragma warning restore 649

        private readonly Dictionary<TKey, TValue> m_dict = new();

        protected internal override void Load()
        {
            base.Load();
            foreach (Node head in m_table.Buckets)
            {
                Node node = head;
                while (node != null)
                {
                    m_dict.Add(node.Key, node.Value);
                    node = node.Next;
                }
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

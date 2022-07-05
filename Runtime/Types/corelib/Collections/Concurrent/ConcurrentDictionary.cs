using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Il2CppToolkit.Runtime.Types.corelib.Collections.Generic;

namespace Il2CppToolkit.Runtime.Types.corelib.Collections.Concurrent
{
	[TypeMapping(typeof(ConcurrentDictionary<,>))]
	public class Native__ConcurrentDictionary<TKey, TValue> : RuntimeObject, IReadOnlyDictionary<TKey, TValue>, INullConstructable
	{
		public Native__ConcurrentDictionary(IMemorySource source, ulong address)
			: base(source, address)
		{
		}

		public class Node : RuntimeObject
		{
			public Node(IMemorySource source, ulong address) : base(source, address) { }
			public TKey Key => Source.ReadValue<TKey>(Address + 0x10ul, 1);
			public TValue Value => Source.ReadValue<TValue>(Address + 0x18ul, 1);
			public Node Next => Source.ReadValue<Node>(Address + 0x20ul, 1);
		}

		public class Table : RuntimeObject
		{
			public Table(IMemorySource source, ulong address) : base(source, address) { }
			public Native__Array<Node> Buckets => Source.ReadValue<Native__Array<Node>>(Address + 0x10ul, 1);
		}

		private Table m_table => Source.ReadValue<Table>(Address + 0x10ul, 1);

		[Ignore]
		private readonly Dictionary<TKey, TValue> m_dict = new();

		private void Load()
		{
			if (Address == 0)
				return;

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

using System;
using System.Collections;
using System.Collections.Generic;

namespace Il2CppToolkit.Runtime.Types.corelib.Collections.Generic
{
	[TypeMapping(typeof(Dictionary<,>))]
	public class Native__Dictionary<TKey, TValue> : RuntimeObject, IReadOnlyDictionary<TKey, TValue>, INullConstructable
	{
		public Native__Dictionary(IMemorySource source, ulong address)
			: base(source, address)
		{
		}

		public struct Entry : IRuntimeObject
		{
			static readonly bool IsNarrow = Il2CsRuntimeContext.GetTypeSize(typeof(TValue)) <= 4 && Il2CsRuntimeContext.GetTypeSize(typeof(TKey)) <= 4;
			public static ulong ElementSize = IsNarrow ? 0x10ul : 0x18ul;

			public IMemorySource Source { get; }
			public ulong Address { get; }

			public Entry(IMemorySource source, ulong address)
			{
				Source = source;
				Address = address;
			}

			public Int32 HashCode => Source.ReadValue<Int32>(Address, 1);
			public Int32 Next => Source.ReadValue<Int32>(Address + 0x04, 1);
			public TKey Key => Source.ReadValue<TKey>(Address + 0x08, 1);
			public TValue Value => Source.ReadValue<TValue>(Address + (IsNarrow ? 0x0Cul : 0x10ul), 1);
		}
		private readonly Dictionary<TKey, TValue> m_dict = new();

		private bool m_isLoaded = false;
		private void Load()
		{
			if (Address == 0 || m_isLoaded)
				return;

			m_isLoaded = true;
			uint size = Source.ReadValue<uint>(Address + 0x20);
			// uint free = Source.ReadValue<uint>(Address + 0x28);
			// uint count = size - free;
			Native__Array<Entry> entries = new(Source, Source.ReadPointer(Address + 0x18ul), size, Entry.ElementSize);
			foreach (Entry entry in entries)
			{
				if (entry.HashCode == -1)
					continue;
				m_dict.Add(entry.Key, entry.Value);
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

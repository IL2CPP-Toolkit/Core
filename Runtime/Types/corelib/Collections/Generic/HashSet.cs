using System;
using System.Collections;
using System.Collections.Generic;

namespace Il2CppToolkit.Runtime.Types.corelib.Collections.Generic
{
	[TypeMapping(typeof(HashSet<>))]
	public class Native__HashSet<T> : RuntimeObject, IReadOnlyCollection<T>, INullConstructable
	{
		public Native__HashSet(IMemorySource source, ulong address)
			: base(source, address)
		{
		}

		public struct Entry : IRuntimeObject
		{
			public IMemorySource Source { get; }
			public ulong Address { get; }

			public Entry(IMemorySource source, ulong address)
			{
				Source = source;
				Address = address;
			}

			public UInt32 HashCode => Source.ReadValue<UInt32>(Address, 1);
			public UInt32 Next => Source.ReadValue<UInt32>(Address + 0x04, 1);
			public T Value => Source.ReadValue<T>(Address + 0x08, 1);
		}

		private readonly HashSet<T> m_set = new();

		private void Load()
		{
			if (Address == 0)
				return;

			uint count = Source.ReadValue<uint>(Address + 0x20);
			Native__Array<Entry> entries = new(Source, Source.ReadPointer(Address + 0x18), count);
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

using System;
using System.Collections;
using System.Collections.Generic;

namespace Il2CppToolkit.Runtime.Types.corelib.Collections.Generic
{
	/**
	 * Raw T* array with specified length
	 */
	public class Native__RawArray<T> : RuntimeObject, IReadOnlyList<T>, INullConstructable
	{
		private readonly ulong m_length;
		private readonly ulong m_elementSize;
		private bool m_isLoaded;
		private CachedMemoryBlock m_cache;
		private readonly List<T> m_items = new();

		public Native__RawArray(IMemorySource source, ulong address, ulong length)
			: base(source, address)
		{
			m_length = length;
			m_elementSize = Il2CsRuntimeContext.GetTypeSize(typeof(T));
		}

		private List<T> Items
		{
			get
			{
				Load();
				return m_items;
			}
		}

		public T[] Array
		{
			get
			{
				Load();
				return m_items.ToArray();
			}
		}

		protected internal void Load()
		{
			if (Address == 0)
				return;

			if (m_isLoaded)
				return;

			m_isLoaded = true;

			m_cache = Source.ParentContext.CacheMemory(Address, m_elementSize * m_length);
			for (ulong index = 0; index < m_length; ++index)
			{
				T value = m_cache.ReadValue<T>(Address + index * m_elementSize);
				m_items.Add(value);
			}
		}

		public IEnumerator<T> GetEnumerator()
		{
			return Items.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return Items.GetEnumerator();
		}

		public int Count => Items.Count;

		public T this[int index] => Items[index];
	}
}

using System.Collections;
using System.Collections.Generic;

namespace Il2CppToolkit.Runtime.Types.corelib.Collections.Generic
{
	[TypeMapping(typeof(List<>))]
	public class Native__List<T> : RuntimeObject, IReadOnlyList<T>, INullConstructable
	{
		private IReadOnlyList<T> m_list;

		public Native__List(IMemorySource source, ulong address) : base(source, address)
		{
		}

		private bool m_isLoaded = false;
		private void Load()
		{
			if (Address == 0)
			{
				m_list = new List<T>();
				return;
			}
			if (m_isLoaded)
				return;

			m_isLoaded = true;
			uint count = (uint)Source.ReadValue<int>(Address + 0x18);
			Native__Array<T> entries = new(Source, Source.ReadPointer(Address + 0x10), count);
			m_list = entries;
		}

		public IEnumerator<T> GetEnumerator()
		{
			Load();
			return m_list.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			Load();
			return ((IEnumerable)m_list).GetEnumerator();
		}

		public int Count
		{
			get
			{
				Load();
				return m_list.Count;
			}
		}

		public T this[int index]
		{
			get
			{
				Load();
				return m_list[index];
			}
		}
	}
}

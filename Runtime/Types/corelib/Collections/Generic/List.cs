using System.Collections;
using System.Collections.Generic;

namespace IL2CS.Runtime.Types.corelib.Collections.Generic
{
	[TypeMapping(typeof(List<>))]
	public class Native__List<T> : StructBase, IReadOnlyList<T>
	{
		private IReadOnlyList<T> m_list;

		public Native__List(Il2CsRuntimeContext context, ulong address) : base(context, address)
		{
		}

		private void ReadFields(Il2CsRuntimeContext context, ulong address)
		{
			uint count = (uint)context.ReadValue<int>(address + 0x18);
			Native__Array<T> entries = new(context, context.ReadPointer(address + 0x10), count);
			entries.Load();
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

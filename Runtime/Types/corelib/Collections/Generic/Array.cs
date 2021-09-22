using System;
using System.Collections;
using System.Collections.Generic;

namespace IL2CS.Runtime.Types.corelib.Collections.Generic
{
	/**
	 * Array structure:
	 * 0x00 - IL2CppObject obj { klass*, monitor* }
	 * 0x10 - IL2CppArrayBounds bounds* {size_t length, size_t lower_bound}
	 * 0x18 - size_t max_length
	 * 0x20 - T items[]
	 */
	public class Native__Array<T> : StructBase, IReadOnlyList<T>
	{
		private readonly ulong? m_specifiedSize;
		private readonly List<T> m_items = new();

		public Native__Array(Il2CsRuntimeContext context, ulong address)
			: base(context, address)
		{
			m_specifiedSize = 0;
		}

		public Native__Array(Il2CsRuntimeContext context, ulong address, ulong size)
			: base(context, address)
		{
			m_specifiedSize = size;
		}

		private void ReadFields(Il2CsRuntimeContext context, ulong address)
		{
			ulong readLength = m_specifiedSize ?? context.ReadValue<ulong>(address + 0x18);
			if (readLength == 0)
			{
				return;
			}

			ulong typeSize = Il2CsRuntimeContext.GetTypeSize(typeof(T));
			MemoryCacheEntry entry = context.CacheMemory(address + 0x20, typeSize * readLength);
			for (ulong index = 0; index < readLength; ++index)
			{
				T value = context.ReadValue<T>(address + 0x20 + index * typeSize);
				if (value == null)
				{
					return;
				}
				m_items.Add(value);
			}
		}

		public IEnumerator<T> GetEnumerator()
		{
			return m_items.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return m_items.GetEnumerator();
		}

		public int Count
		{
			get { return m_items.Count; }
		}

		public T this[int index]
		{
			get { return m_items[index]; }
		}
	}
}

using System;
using System.Collections;
using System.Collections.Generic;

namespace Il2CppToolkit.Runtime.Types.corelib.Collections.Generic
{
    /**
	 * Raw T* array with specified length
	 */
    public class Native__RawArray<T> : StructBase, IReadOnlyList<T>, INullConstructable
    {
        private readonly ulong m_length;
        private readonly ulong m_elementSize;
        private readonly List<T> m_items = new();

        public Native__RawArray(IMemorySource source, ulong address, ulong length)
            : base(source, address)
        {
            m_length = length;
            m_elementSize = Il2CsRuntimeContext.GetTypeSize(typeof(T));
        }

        public Native__RawArray(IMemorySource source, ulong address, ulong length, ulong elementSize)
            : base(source, address)
        {
            m_length = length;
            m_elementSize = elementSize;
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

        protected internal override void Load()
        {
            if (Address == 0)
                return;

            if (m_isLoaded)
                return;

            m_isLoaded = true;

            ulong typeSize = m_elementSize;
            m_cache = MemorySource.ParentContext.CacheMemory(Address, typeSize * m_length);
            for (ulong index = 0; index < m_length; ++index)
            {
                T value = m_cache.ReadValue<T>(Address + index * typeSize);
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

        public int Count
        {
            get { return Items.Count; }
        }

        public T this[int index]
        {
            get { return Items[index]; }
        }
    }
}

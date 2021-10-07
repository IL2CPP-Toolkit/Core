﻿using System;
using System.Collections;
using System.Collections.Generic;

namespace Il2CppToolkit.Runtime.Types.corelib.Collections.Generic
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

        public Native__Array(IMemorySource source, ulong address)
            : base(source, address)
        {
            m_specifiedSize = null;
        }

        public Native__Array(IMemorySource source, ulong address, ulong size)
            : base(source, address)
        {
            m_specifiedSize = size;
        }

        private List<T> Items
        {
            get
            {
                Load();
                return m_items;
            }
        }

        protected internal override void Load()
        {
            if (m_isLoaded)
            {
                return;
            }
            m_isLoaded = true;

            ulong readLength = m_specifiedSize ?? MemorySource.ReadValue<ulong>(Address + 0x18);
            if (readLength == 0)
            {
                return;
            }

            ulong typeSize = Il2CsRuntimeContext.GetTypeSize(typeof(T));
            m_cache = MemorySource.ParentContext.CacheMemory(Address + 0x20, typeSize * readLength);
            for (ulong index = 0; index < readLength; ++index)
            {
                T value = m_cache.ReadValue<T>(Address + 0x20 + index * typeSize);
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

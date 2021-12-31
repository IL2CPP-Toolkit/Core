using System.Collections;
using System.Collections.Generic;

namespace Il2CppToolkit.Runtime.Types.corelib.Collections.Generic
{
    [TypeMapping(typeof(Stack<>))]
    public class Native__Stack<T> : StructBase, IReadOnlyCollection<T>, INullConstructable
    {
        public Native__Stack(IMemorySource source, ulong address)
            : base(source, address)
        {
        }

        private readonly Stack<T> m_stack = new();

        private void ReadFields(IMemorySource source, ulong address)
        {
            if (Address == 0)
                return;

            uint count = source.ReadValue<uint>(address + 0x18);
            Native__Array<T> entries = new(source, source.ReadPointer(address + 0x10), count);
            entries.Load();
            foreach (T entry in entries)
            {
                m_stack.Push(entry);
            }
        }

        public Stack<T> UnderlyingStack
        {
            get
            {
                Load();
                return m_stack;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return UnderlyingStack.GetEnumerator();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return UnderlyingStack.GetEnumerator();
        }

        public int Count
        {
            get { return UnderlyingStack.Count; }
        }
    }
}

using System.Collections;
using System.Collections.Generic;

namespace Il2CppToolkit.Runtime.Types.corelib.Collections.Generic
{
	[TypeMapping(typeof(Stack<>))]
	public class Native__Stack<T> : RuntimeObject, IReadOnlyCollection<T>, INullConstructable
	{
		public Native__Stack(IMemorySource source, ulong address)
			: base(source, address)
		{
		}

		private readonly Stack<T> m_stack = new();

		private void Load()
		{
			if (Address == 0)
				return;

			uint count = Source.ReadValue<uint>(Address + 0x18);
			Native__Array<T> entries = new(Source, Source.ReadPointer(Address + 0x10), count);
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

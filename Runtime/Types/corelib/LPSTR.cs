using System;
using System.Text;

namespace Il2CppToolkit.Runtime.Types.corelib
{
	public class Native__LPSTR : RuntimeObject
	{
		public Native__LPSTR() : base() { }
		public Native__LPSTR(IMemorySource source, ulong address) : base(source, address) { }

		private string m_value;
		public string Value
		{
			get
			{
				if (m_value == null)
				{
					ulong address = Source.ReadPointer(Address);
					ReadOnlyMemory<byte> stringData = Source.ReadMemory(Address, 512);
#if NET472
					m_value = Encoding.UTF8.GetString(stringData.Span.ToArray()).Split(new char[] { '\0' }, 2)[0];
#else
                    m_value = Encoding.UTF8.GetString(stringData.Span).Split(new char[] { '\0' }, 2)[0];
#endif
				}
				return m_value;
			}
		}
	}
}

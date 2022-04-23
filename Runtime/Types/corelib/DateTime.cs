using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Il2CppToolkit.Runtime.Types.corelib
{
	[TypeMapping(typeof(DateTime))]
	public struct Native__DateTime
	{
		private static ulong NativeSize => sizeof(long);

		[Offset(0)]
#pragma warning disable 649
		private long m_value;
#pragma warning restore 649

		public long BinaryValue => m_value;

		public DateTime Value
		{
			get
			{
				return DateTime.FromBinary(m_value).ToLocalTime();
			}
		}
	}
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IL2CS.Core;

namespace IL2CS.Runtime.Types.corelib
{
	[TypeMapping(typeof(DateTime))]
	public struct Native__DateTime
	{
		private static ulong NativeSize => sizeof(ulong);

		[Offset(0)]
#pragma warning disable 649
		private ulong m_value;
#pragma warning restore 649

		public DateTime Value
		{
			get
			{
				return new DateTime((long)m_value);
			}
		}
	}
}
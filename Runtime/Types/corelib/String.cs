using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IL2CS.Runtime.Types.corelib
{
	public struct Native__String
	{
		public string Value;
		private void ReadFields(Il2CsRuntimeContext context, ulong address)
		{
			int strlen = context.ReadValue<int>(address + 16);
			if (strlen <= 0)
			{
				Value = string.Empty;
				DebugHelpers.Assert(strlen == 0, "Invalid string length");
				return;
			}

			ReadOnlyMemory<byte> stringData = context.ReadMemory(address + 20, (ulong)strlen * 2);
			Value = Encoding.Unicode.GetString(stringData.Span);
		}
	}
}

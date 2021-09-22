using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IL2CS.Runtime
{
	public static class Utilities
	{
		public static ulong GetTypeTag(int imageTypeStart, uint typeToken)
		{
			return ((ulong)imageTypeStart << 32) + typeToken;
		}
	}
}

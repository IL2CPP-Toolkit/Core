using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IL2CS.Runtime
{
	public static class DebugHelpers
	{
		public static void VerifyElseThrow(bool condition, string message)
		{
			if (condition)
			{
				return;
			}

			string errorMessage = $"Fatal error: {message}";
			Trace.WriteLine(errorMessage);
			if (Debugger.IsAttached)
			{
				Debugger.Break();
			}

			throw new ApplicationException(errorMessage);
		}

		public static void Assert(bool condition, string message)
		{
			if (condition)
			{
				return;
			}

			Trace.WriteLine($"Assertion failed: {message}");
			if (Debugger.IsAttached)
			{
				Debugger.Break();
			}

		}
	}
}
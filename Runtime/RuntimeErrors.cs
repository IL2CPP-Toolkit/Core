using ProcessMemoryUtilities.Managed;
using ProcessMemoryUtilities.Native;
using System;
using System.Reflection;
using Il2CppToolkit.Common.Errors;
using Il2CppToolkit.Runtime.Types.Reflection;

namespace Il2CppToolkit.Runtime
{
	[ErrorCategory("Runtime Error", "RT")]
	public enum RuntimeError
	{
		[ErrorSeverity(ErrorSeverity.Error)] OffsetRequired = 1,
		[ErrorSeverity(ErrorSeverity.Error)] ReadProcessMemoryReadFailed,
		[ErrorSeverity(ErrorSeverity.Error)] ReadProcessMemoryReadArrayFailed,
		[ErrorSeverity(ErrorSeverity.Error)] ReadProcessMemoryCacheRangeError,
		[ErrorSeverity(ErrorSeverity.Error)] StaticAddressMissing,
	}
}
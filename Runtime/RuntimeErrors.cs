using Il2CppToolkit.Common.Errors;

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
        [ErrorSeverity(ErrorSeverity.Error)] WriteProcessMemoryWriteFailed,
    }
}
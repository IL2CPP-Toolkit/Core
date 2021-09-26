using Il2CppToolkit.Common.Errors;

namespace Il2CppToolkit.Model
{
    [ErrorCategory("Compiler Error", "CE")]
    public enum CompilerError
    {
        [ErrorSeverity(ErrorSeverity.Fatal)] InternalError = 1,
    }
}
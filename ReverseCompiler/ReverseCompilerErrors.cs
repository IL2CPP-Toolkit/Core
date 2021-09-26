using Il2CppToolkit.Common.Errors;

namespace Il2CppToolkit.Model
{
    [ErrorCategory("Compiler Error", "CE")]
    public enum CompilerError
    {
        [ErrorSeverity(ErrorSeverity.Fatal)] InternalError = 1,
        [ErrorSeverity(ErrorSeverity.Error)] ILGenerationError,

        [ErrorSeverity(ErrorSeverity.Warning)] UnknownTypeReference = 500,
        [ErrorSeverity(ErrorSeverity.Warning)] IncompleteGenericType,
        [ErrorSeverity(ErrorSeverity.Warning)] InterfaceNotSupportedOrEmitted,
        [ErrorSeverity(ErrorSeverity.Warning)] UnknownType,

    }
}
using System;
using System.Diagnostics;

namespace Il2CppToolkit.Common.Errors
{
    public static class ErrorHandler
    {
        public class ErrorEventArgs : EventArgs
        {
            public StructuredExceptionBase Exception;
            public ErrorEventArgs(StructuredExceptionBase ex) => Exception = ex;
        }

        public static ErrorSeverity ErrorThreshhold { get; set; } = ErrorSeverity.Error;

        public static event EventHandler<ErrorEventArgs> OnError;

        public static void HandleError<TError>(StructuredException<TError> ex) where TError : Enum
        {
            OnError.Invoke(null, new(ex));
            if (ex.Severity >= ErrorHandler.ErrorThreshhold)
            {
                throw ex;
            }
        }

        public static void VerifyElseThrow<TError>(bool condition, TError errorCode, string message) where TError : Enum
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
            errorCode.Raise(message);
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
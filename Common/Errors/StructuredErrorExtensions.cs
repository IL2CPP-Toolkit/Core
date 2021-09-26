using System;
using System.Reflection;
using System.Collections.Generic;
using Il2CppToolkit.Common.Errors;

namespace Il2CppToolkit
{
    public static class StructuredErrorExtensions
    {
        private static class StructuredErrorData<TError> where TError : Enum
        {
            public static ErrorCategoryAttribute ErrorCategory;
            public static Dictionary<TError, ErrorSeverity> SeverityMap = new();
            static StructuredErrorData()
            {
                Type errorType = typeof(TError);
                ErrorCategoryAttribute attr = errorType.GetCustomAttribute<ErrorCategoryAttribute>();
                if (attr == null)
                {
                    throw new InvalidOperationException($"Error enumeration '${errorType.FullName}' does not have required 'ErrorCategoryAttribute'");
                }
                ErrorCategory = attr;

                FieldInfo[] fieldInfos = errorType.GetFields(BindingFlags.Public | BindingFlags.Static);
                foreach (FieldInfo fieldInfo in fieldInfos)
                {
                    ErrorSeverityAttribute attrSeverity = fieldInfo.GetCustomAttribute<ErrorSeverityAttribute>();
                    SeverityMap.Add((TError)fieldInfo.GetRawConstantValue(), attrSeverity?.Severity ?? ErrorSeverity.Error);
                }
            }
        }

        public static ErrorSeverity GetSeverity<TError>(this TError errorCode) where TError : Enum
        {
            return StructuredErrorData<TError>.SeverityMap[errorCode];
        }

        public static ErrorCategoryAttribute GetCategory<TError>(this TError errorCode) where TError : Enum
        {
            return StructuredErrorData<TError>.ErrorCategory;
        }

        public static string GetName<TError>(this TError errorCode) where TError : Enum
        {
            return $"{StructuredErrorData<TError>.ErrorCategory.Abbreviation}{(int)(object)errorCode}";
        }

        public static void Raise<TError>(this TError errorCode, string message) where TError : Enum
        {
            ErrorSeverity sev = errorCode.GetSeverity();
            ErrorHandler.HandleError(new StructuredException<TError>(errorCode, message));
        }
    }
}
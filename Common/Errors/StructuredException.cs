using System;

namespace Il2CppToolkit.Common.Errors
{
	public class StructuredExceptionBase : Exception
	{
		public ErrorCategoryAttribute Category { get; }
		public ErrorSeverity Severity { get; }
		public int ErrorCode { get; }

		public StructuredExceptionBase(int errorCode, ErrorSeverity severity, ErrorCategoryAttribute category, string message) : base(message)
		{
			ErrorCode = errorCode;
			Severity = severity;
			Category = category;
		}

		public override string ToString()
		{
			return $"{Severity.ToString().ToLowerInvariant()} {Category.Abbreviation}{ErrorCode}: {Message}";
		}
	}

	public class StructuredException<TError> : StructuredExceptionBase where TError : Enum
	{
		public StructuredException(TError errorCode, string message) : base((int)(object)errorCode, errorCode.GetSeverity(), errorCode.GetCategory(), message)
		{
		}
	}
}
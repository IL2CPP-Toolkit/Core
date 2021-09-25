using System;

namespace Il2CppToolkit.Common.Errors
{
	[AttributeUsage(AttributeTargets.Field)]
	public class ErrorSeverityAttribute : Attribute
	{
		public ErrorSeverity Severity { get; }
		public ErrorSeverityAttribute(ErrorSeverity severity)
		{
			Severity = severity;
		}
	}
}
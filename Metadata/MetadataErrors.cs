using Il2CppToolkit.Common.Errors;

namespace Il2CppToolkit.Model
{
	[ErrorCategory("Metadata Error", "MD")]
	public enum MetadataError
	{
		[ErrorSeverity(ErrorSeverity.Fatal)] ConfigurationError = 1,
		[ErrorSeverity(ErrorSeverity.Fatal)] UnknownFormat,
	}
}
using System;

namespace Il2CppToolkit.ReverseCompiler
{
	public class ProgressUpdatedEventArgs : EventArgs
	{
		public string? DisplayName { get; set; }
		public int Completed { get; set; }
		public int Total { get; set; }
	}
}
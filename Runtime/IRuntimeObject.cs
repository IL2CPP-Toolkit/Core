namespace Il2CppToolkit.Runtime
{
	public interface IRuntimeObject
	{
		IMemorySource Source { get; }
		ulong Address { get; }
	}
}
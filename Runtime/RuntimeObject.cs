namespace Il2CppToolkit.Runtime
{
	public abstract class RuntimeObject : IRuntimeObject
	{
		public IMemorySource Source { get; }
		public ulong Address { get; }

		public RuntimeObject(IMemorySource source, ulong address)
		{
			Source = source;
			Address = address;
		}
	}
}
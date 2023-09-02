namespace Il2CppToolkit.Runtime;

public abstract class RuntimeObject : IRuntimeObject
{
	public IMemorySource Source { get; }
	public ulong Address { get; }

	public RuntimeObject() { }

	public RuntimeObject(IMemorySource source, ulong address)
	{
		Source = source;
		Address = address;
	}
}

public class ObjectPointer : RuntimeObject
{
	public ObjectPointer() : base() { }

	public ObjectPointer(IMemorySource source, ulong address)
		: base(source, address)
	{ }
}
using System;

namespace Il2CppToolkit.Runtime.Types
{
	public interface ITypeFactory
	{
		object ReadValue(IMemorySource source, ulong address);
		void WriteValue(IMemorySource source, ulong address, object value);
	}
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = true, AllowMultiple = false)]
	public class TypeFactoryAttribute : Attribute
	{
		public Type Type { get; private set; }
		public TypeFactoryAttribute(Type type)
		{
			Type = type;
		}
	}
}

using System;

namespace Il2CppToolkit.Runtime.Types
{
	public interface ITypeFactory
	{
		object ReadValue(IMemorySource source, ulong address);
	}
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = true, AllowMultiple = false)]
	public class TypeFactoryAttribute : Attribute
	{
		public Type Type { get; private set; }
		public TypeFactoryAttribute(Type type)
		{
			if (!type.IsAssignableTo(typeof(ITypeFactory)))
				throw new ArgumentException("Class marked with [TypeFactoryAttribute] must extend ITypeFactory", nameof(type));
			Type = type;
		}
	}
}

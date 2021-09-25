using System;

namespace Il2CppToolkit.Runtime.Types
{
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = true, AllowMultiple = false)]
	public class TypeMappingAttribute : Attribute
	{
		public Type Type { get; private set; }
		public TypeMappingAttribute(Type offset)
		{
			Type = offset;
		}
	}
}

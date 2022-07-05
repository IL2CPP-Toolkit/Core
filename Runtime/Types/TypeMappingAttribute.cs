using System;

namespace Il2CppToolkit.Runtime.Types
{
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = true, AllowMultiple = false)]
	public class TypeMappingAttribute : Attribute
	{
		public Type Type { get; }
		public TypeMappingAttribute(Type type)
		{
			Type = type;
		}
	}
}

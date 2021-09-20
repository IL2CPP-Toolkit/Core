using System.Reflection;
using System.Text.RegularExpressions;

namespace Il2CppToolkit.Model
{
	public class FieldDescriptor
	{
		public FieldDescriptor(string name, ITypeReference typeReference, FieldAttributes attrs, ulong offset)
		{
			Name = name;
			Type = typeReference;
			Attributes = attrs;
			Offset = offset;
		}

		public readonly string Name;
		public readonly ITypeReference Type;
		public FieldAttributes Attributes;
		public readonly ulong Offset;
		public object DefaultValue;
	}
}

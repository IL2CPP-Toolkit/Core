using System;
using System.Linq;

namespace Il2CppToolkit.Model
{
	public interface ITypeReference
	{
		public string Name { get; }
	}

	public class DotNetTypeReference : ITypeReference
	{
		public string Name { get; }
		public Type Type { get; }
		DotNetTypeReference(Type type)
		{
			Name = type.FullName;
			Type = type;
		}
	}

	public class GenericTypeReference : ITypeReference
	{
		public string Name { get; }
		public ITypeReference GenericType { get; }
		public ITypeReference[] TypeArguments { get; }
		public GenericTypeReference(ITypeReference genericType, params ITypeReference[] typeArguments)
		{
			Name = $"{genericType.Name}[{string.Join(", ", typeArguments.Select(arg => arg.Name))}]";
			GenericType = genericType;
			TypeArguments = typeArguments;
		}
	}

	public class Il2CppTypeReference : ITypeReference
	{
		public string Name { get; }
		public Il2CppType CppType { get; }
		public TypeDescriptor TypeContext { get; }
		public Il2CppTypeReference(string typeName, Il2CppType cppType, TypeDescriptor typeContext)
		{
			Name = typeName;
			CppType = cppType;
			TypeContext = typeContext;
		}
	}

	public class TypeDescriptorReference : ITypeReference
	{
		public string Name { get; }
		public TypeDescriptor Descriptor { get; }
		public TypeDescriptorReference(TypeDescriptor descriptor)
		{
			Name = descriptor.Name;
			Descriptor = descriptor;
		}
	}
}

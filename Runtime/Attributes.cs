using System;

namespace IL2CS.Core
{
	[AttributeUsage(AttributeTargets.Assembly)]
	public class GeneratedAttribute : Attribute
	{
		public GeneratedAttribute()
		{
		}
	}

	[AttributeUsage(AttributeTargets.Class)]
	public class StaticAttribute : Attribute
	{
		public StaticAttribute()
		{
		}
	}

	[AttributeUsage(AttributeTargets.Field)]
	public class IgnoreAttribute : Attribute
	{
	}

	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
	public class TokenAttribute : Attribute
	{
		public uint Token { get; }
		public TokenAttribute(uint token)
		{
			Token = token;
		}
	}

	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
	public class TagAttribute : Attribute
	{
		public ulong Tag { get; }
		public TagAttribute(ulong token)
		{
			Tag = token;
		}
	}

	[AttributeUsage(AttributeTargets.Field)]
	public class OffsetAttribute : Attribute
	{
		public ulong OffsetBytes { get; }
		public OffsetAttribute(ulong offset)
		{
			OffsetBytes = offset;
		}
	}

	[AttributeUsage(AttributeTargets.Field)]
	public class IndirectionAttribute : Attribute
	{
		public byte Indirection { get; }
		public IndirectionAttribute(byte indirection)
		{
			Indirection = indirection;
		}
	}

	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Field)]
	public class AddressAttribute : Attribute
	{
		public ulong Address { get; }
		public string RelativeToModule { get; }
		public AddressAttribute(ulong address, string relativeToModule)
		{
			Address = address;
			RelativeToModule = relativeToModule;
		}
	}

	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
	public class SizeAttribute : Attribute
	{
		public uint Size { get; }
		public SizeAttribute(uint size)
		{
			Size = size;
		}
	}
}
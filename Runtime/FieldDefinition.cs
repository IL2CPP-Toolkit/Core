using System;
using System.Collections.Generic;

namespace Il2CppToolkit.Runtime
{
	public class FieldMember<TClass, TValue>
		where TClass : IRuntimeObject
	{
		private readonly ulong __offset;
		private readonly byte __indirection;
		public FieldMember(ulong offset, byte indirection = 1)
		{
			__offset = offset;
			__indirection = indirection;
		}
		public TValue GetValue(TClass obj)
		{
			return obj.Source.ReadValue<TValue>(obj.Address + __offset, __indirection);
		}
	}

	public class StaticFieldMember<TClass, TValue>
	{
		private readonly string __moduleName;
		private readonly ulong __clsOffset;
		private readonly ulong __offset;
		private readonly byte __indirection;
		public StaticFieldMember(string moduleName, ulong clsOffset, ulong offset, byte indirection = 1)
		{
			__moduleName = moduleName;
			__clsOffset = clsOffset;
			__offset = offset;
			__indirection = indirection;
		}
		public TValue GetValue(IMemorySource source)
		{
			ulong modOffset = source.ParentContext.GetModuleAddress(__moduleName);
			// var classDef = source.ReadValue<Il2CppToolkit.Runtime.Types.Reflection.ClassDefinition>(modOffset + __clsOffset);
			ulong staticFields = source.ReadPointer(source.ReadPointer(modOffset + __clsOffset) + 0xB8);
			return source.ReadValue<TValue>(staticFields + __offset, __indirection);
		}
	}
}
using System;

namespace Il2CppToolkit.Runtime.Types.corelib
{
	[TypeFactory(typeof(Nullable<>))]
	public class NullableFactory<T> : ITypeFactory where T : struct
	{
		public object ReadValue(IMemorySource source, ulong address)
		{
			UnknownObject obj = new(source, address);
			bool hasValue = Il2CppTypeInfoLookup<Nullable<T>>.GetValue<bool>(obj, "has_value");
			T value = hasValue ? Il2CppTypeInfoLookup<Nullable<T>>.GetValue<T>(obj, "value") : default;
			return hasValue ? new Nullable<T>(value) : new Nullable<T>();
		}

		public void WriteValue(IMemorySource source, ulong address, object value)
		{
			throw new NotImplementedException();
		}
	}
}

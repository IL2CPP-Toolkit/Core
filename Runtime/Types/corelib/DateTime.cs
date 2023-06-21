using System;

namespace Il2CppToolkit.Runtime.Types.corelib
{
	[TypeFactory(typeof(DateTime))]
	public class DateTimeFactory : ITypeFactory
	{
		public object ReadValue(IMemorySource source, ulong address)
		{
			UnknownObject obj = new(source, address);
			long dateData = Il2CppTypeInfoLookup<DateTime>.GetValue<long>(obj, "_dateData");
			return DateTime.FromBinary(dateData).ToLocalTime();
		}

		public void WriteValue(IMemorySource source, ulong address, object value)
		{
			throw new NotImplementedException();
		}
	}
}
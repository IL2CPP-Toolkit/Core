using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Il2CppToolkit.Runtime.Types.corelib
{
	[TypeFactory(typeof(DateTime))]
	public class DateTimeFactory : ITypeFactory
	{
		public object ReadValue(IMemorySource source, ulong address)
		{
			UnknownObject obj = new(source, address);
			long dateData = Il2CppTypeInfoLookup<DateTime>.GetValue<long>(obj, "dateData");
			return DateTime.FromBinary(dateData).ToLocalTime();
		}
	}

	//[Obsolete]
	//[TypeMapping(typeof(DateTime))]
	//public struct Native__DateTime : IRuntimeObject
	//{
	//	public Native__DateTime() => (Source, Address) = (default, default);
	//	public Native__DateTime(IMemorySource source, ulong address) => (Source, Address) = (source, address);
	//	public IMemorySource Source { get; }
	//	public ulong Address { get; }

	//	// private fields
	//	private long dateData => Il2CppTypeInfoLookup<DateTime>.GetValue<long>(this, nameof(dateData));
		
	//	// properties
	//	public DateTime Value => DateTime.FromBinary(dateData).ToLocalTime();
	//	public long BinaryValue => dateData;
	//}
}
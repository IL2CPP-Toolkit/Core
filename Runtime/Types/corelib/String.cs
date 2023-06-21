using System;
using System.Linq;
using System.Text;
using Il2CppToolkit.Common.Errors;

namespace Il2CppToolkit.Runtime.Types.corelib
{
	[TypeFactory(typeof(string))]
	public class StringFactory : ITypeFactory
	{
		public object ReadValue(IMemorySource source, ulong address)
		{
			UnknownObject obj = new(source, address);
			int len = Il2CppTypeInfoLookup<string>.GetValue<int>(obj, "_stringLength");

			if (len <= 0)
			{
				ErrorHandler.Assert(len == 0, "Invalid string length");
				return string.Empty;
			}

			var typeInfo = Il2CppTypeCache.GetTypeInfo(source.ParentContext, typeof(string));
			ReadOnlyMemory<byte> stringData = source.ReadMemory(
				address + typeInfo.Fields.First(fld => fld.Name == "_firstChar").Offset,
				(ulong)len * 2);

#if NET472
			return Encoding.Unicode.GetString(stringData.Span.ToArray());
#else
			return Encoding.Unicode.GetString(stringData.Span);
#endif
		}

		public void WriteValue(IMemorySource source, ulong address, object value)
		{
			throw new NotSupportedException("Cannot write directly to string buffer");
		}
	}
}

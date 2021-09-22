using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IL2CS.Runtime
{
	public static class BitReader
	{
		private static readonly Dictionary<Type, Func<Il2CsRuntimeContext, ulong, object>> s_impl = new ();
		static BitReader()
		{
			// ReSharper disable BuiltInTypeReferenceStyle
			s_impl.Add(typeof(Char), (context, address) => BitConverter.ToChar(context.ReadMemory(address, sizeof(Char)).Span));
			s_impl.Add(typeof(Boolean), (context, address) => BitConverter.ToBoolean(context.ReadMemory(address, sizeof(Boolean)).Span));
			s_impl.Add(typeof(Double), (context, address) => BitConverter.ToDouble(context.ReadMemory(address, sizeof(Double)).Span));
			s_impl.Add(typeof(Single), (context, address) => BitConverter.ToSingle(context.ReadMemory(address, sizeof(Single)).Span));
			s_impl.Add(typeof(Int16), (context, address) => BitConverter.ToInt16(context.ReadMemory(address, sizeof(Int16)).Span));
			s_impl.Add(typeof(Int32), (context, address) => BitConverter.ToInt32(context.ReadMemory(address, sizeof(Int32)).Span));
			s_impl.Add(typeof(Int64), (context, address) => BitConverter.ToInt64(context.ReadMemory(address, sizeof(Int64)).Span));
			s_impl.Add(typeof(UInt16), (context, address) => BitConverter.ToUInt16(context.ReadMemory(address, sizeof(UInt16)).Span));
			s_impl.Add(typeof(UInt32), (context, address) => BitConverter.ToUInt32(context.ReadMemory(address, sizeof(UInt32)).Span));
			s_impl.Add(typeof(UInt64), (context, address) => BitConverter.ToUInt64(context.ReadMemory(address, sizeof(UInt64)).Span));
			// ReSharper restore BuiltInTypeReferenceStyle
		}
		public static object ReadPrimitive(this Il2CsRuntimeContext context, Type type, ulong address)
		{
			if (s_impl.TryGetValue(type, out Func<Il2CsRuntimeContext, ulong, object> fn))
			{
				return fn(context, address);
			}
			throw new ArgumentException($"Type '{type.FullName}' is not a valid primitive type");
		}
	}
}

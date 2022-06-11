using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System
{
	public static class ReadOnlyMemoryExtensions
	{
		public static char ToChar(this ReadOnlyMemory<byte> memory)
		{
#if NET472
			return BitConverter.ToChar(memory.Span.ToArray(), (int)0);
#else
			return BitConverter.ToChar(memory.Span);
#endif
		}
		public static bool ToBoolean(this ReadOnlyMemory<byte> memory)
		{
#if NET472
			return BitConverter.ToBoolean(memory.Span.ToArray().ToArray(), (int)0);
#else
			return BitConverter.ToBoolean(memory.Span);
#endif
		}
		public static double ToDouble(this ReadOnlyMemory<byte> memory)
		{
#if NET472
			return BitConverter.ToDouble(memory.Span.ToArray(), (int)0);
#else
			return BitConverter.ToDouble(memory.Span);
#endif
		}
		public static float ToSingle(this ReadOnlyMemory<byte> memory)
		{
#if NET472
			return BitConverter.ToSingle(memory.Span.ToArray(), (int)0);
#else
			return BitConverter.ToSingle(memory.Span);
#endif
		}
		public static short ToInt16(this ReadOnlyMemory<byte> memory)
		{
#if NET472
			return BitConverter.ToInt16(memory.Span.ToArray(), (int)0);
#else
			return BitConverter.ToInt16(memory.Span);
#endif
		}
		public static int ToInt32(this ReadOnlyMemory<byte> memory)
		{
#if NET472
			return BitConverter.ToInt32(memory.Span.ToArray(), (int)0);
#else
			return BitConverter.ToInt32(memory.Span);
#endif
		}
		public static long ToInt64(this ReadOnlyMemory<byte> memory)
		{
#if NET472
			return BitConverter.ToInt64(memory.Span.ToArray(), (int)0);
#else
			return BitConverter.ToInt64(memory.Span);
#endif
		}
		public static ushort ToUInt16(this ReadOnlyMemory<byte> memory)
		{
#if NET472
			return BitConverter.ToUInt16(memory.Span.ToArray(), (int)0);
#else
			return BitConverter.ToUInt16(memory.Span);
#endif
		}
		public static uint ToUInt32(this ReadOnlyMemory<byte> memory)
		{
#if NET472
			return BitConverter.ToUInt32(memory.Span.ToArray(), (int)0);
#else
			return BitConverter.ToUInt32(memory.Span);
#endif
		}
		public static ulong ToUInt64(this ReadOnlyMemory<byte> memory)
		{
#if NET472
			return BitConverter.ToUInt64(memory.Span.ToArray(), (int)0);
#else
			return BitConverter.ToUInt64(memory.Span);
#endif
		}
		public static IntPtr ToIntPtr(this ReadOnlyMemory<byte> memory)
		{
#if NET472
			return (IntPtr)BitConverter.ToInt64(memory.Span.ToArray(), (int)0);
#else
			return (IntPtr)BitConverter.ToInt64(memory.Span);
#endif
		}
		public static UIntPtr ToUIntPtr(this ReadOnlyMemory<byte> memory)
		{
#if NET472
			return (UIntPtr)BitConverter.ToInt64(memory.Span.ToArray(), (int)0);
#else
			return (UIntPtr)BitConverter.ToInt64(memory.Span);
#endif
		}
	}
}

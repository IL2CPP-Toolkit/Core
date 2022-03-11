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
			return BitConverter.ToChar(memory.Buffer, (int)memory.Offset);
#else
			return BitConverter.ToChar(memory.Span);
#endif
		}
		public static bool ToBoolean(this ReadOnlyMemory<byte> memory)
		{
#if NET472
			return BitConverter.ToBoolean(memory.Buffer, (int)memory.Offset);
#else
			return BitConverter.ToBoolean(memory.Span);
#endif
		}
		public static double ToDouble(this ReadOnlyMemory<byte> memory)
		{
#if NET472
			return BitConverter.ToDouble(memory.Buffer, (int)memory.Offset);
#else
			return BitConverter.ToDouble(memory.Span);
#endif
		}
		public static float ToSingle(this ReadOnlyMemory<byte> memory)
		{
#if NET472
			return BitConverter.ToSingle(memory.Buffer, (int)memory.Offset);
#else
			return BitConverter.ToSingle(memory.Span);
#endif
		}
		public static short ToInt16(this ReadOnlyMemory<byte> memory)
		{
#if NET472
			return BitConverter.ToInt16(memory.Buffer, (int)memory.Offset);
#else
			return BitConverter.ToInt16(memory.Span);
#endif
		}
		public static int ToInt32(this ReadOnlyMemory<byte> memory)
		{
#if NET472
			return BitConverter.ToInt32(memory.Buffer, (int)memory.Offset);
#else
			return BitConverter.ToInt32(memory.Span);
#endif
		}
		public static long ToInt64(this ReadOnlyMemory<byte> memory)
		{
#if NET472
			return BitConverter.ToInt64(memory.Buffer, (int)memory.Offset);
#else
			return BitConverter.ToInt64(memory.Span);
#endif
		}
		public static ushort ToUInt16(this ReadOnlyMemory<byte> memory)
		{
#if NET472
			return BitConverter.ToUInt16(memory.Buffer, (int)memory.Offset);
#else
			return BitConverter.ToUInt16(memory.Span);
#endif
		}
		public static uint ToUInt32(this ReadOnlyMemory<byte> memory)
		{
#if NET472
			return BitConverter.ToUInt32(memory.Buffer, (int)memory.Offset);
#else
			return BitConverter.ToUInt32(memory.Span);
#endif
		}
		public static ulong ToUInt64(this ReadOnlyMemory<byte> memory)
		{
#if NET472
			return BitConverter.ToUInt64(memory.Buffer, (int)memory.Offset);
#else
			return BitConverter.ToUInt64(memory.Span);
#endif
		}
		public static IntPtr ToIntPtr(this ReadOnlyMemory<byte> memory)
		{
#if NET472
			return (IntPtr)BitConverter.ToInt64(memory.Buffer, (int)memory.Offset);
#else
			return (IntPtr)BitConverter.ToInt64(memory.Span);
#endif
		}
		public static UIntPtr ToUIntPtr(this ReadOnlyMemory<byte> memory)
		{
#if NET472
			return (UIntPtr)BitConverter.ToInt64(memory.Buffer, (int)memory.Offset);
#else
			return (UIntPtr)BitConverter.ToInt64(memory.Span);
#endif
		}
	}

#if NET472
	public class ReadOnlyMemory<T> where T : struct
	{
		public T[] Buffer { get; private set; }
		public ulong Offset { get; private set; }
		public ulong Length { get; private set; }

		public ReadOnlyMemory(T[] initialData)
		{
			Buffer = initialData;
			Offset = 0;
			Length = (ulong)Buffer.Length;
		}

		public ReadOnlyMemory(T[] initialData, ulong offset, ulong length)
		{
			Buffer = initialData;
			Offset = offset;
			Length = length;
		}

		public ReadOnlyMemory<T> Slice(int offset, int length)
		{
			return new ReadOnlyMemory<T>(Buffer, Offset + (ulong)offset, (ulong)length);
		}

		public T[] Span
		{
			get
			{
				T[] copy = new T[Length];
				Array.ConstrainedCopy(Buffer, (int)Offset, copy, 0, (int)Length);
				return copy;
			}
		}
	}
#endif
}

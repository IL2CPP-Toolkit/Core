using System;
using System.Diagnostics;
using System.Reflection;
// ReSharper disable InconsistentNaming

namespace IL2CS.Runtime.Types.corelib
{
	[TypeMapping(typeof(Nullable<>))]
	public struct Native__Nullable<T>
	{
		public T Value { get; private set; }
		public bool HasValue { get; private set; }

		private void ReadFields(Il2CsRuntimeContext context, ulong address)
		{
			ReadOnlyMemory<byte> hasValue = context.ReadMemory(address + Il2CsRuntimeContext.GetTypeSize(typeof(T)), 1);
			HasValue = BitConverter.ToBoolean(hasValue.Span);
			if (!HasValue)
			{
				return;
			}

			Value = (T)context.ReadValue(typeof(T), address);
		}
	}
}

using ProcessMemoryUtilities.Managed;
using ProcessMemoryUtilities.Native;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Il2CppToolkit.Common.Errors;
using static Il2CppToolkit.Runtime.Types.TypeSystem;
using Il2CppToolkit.Runtime.Types.Reflection;
using Il2CppToolkit.Injection.Client;

namespace Il2CppToolkit.Runtime
{
	public class Il2CsRuntimeContext : IMemorySource, IDisposable
	{
		public class ObjectEventArgs : EventArgs
		{
			public ulong Address { get; }
			public object Value { get; }

			public ObjectEventArgs(object obj, ulong address) : base()
			{
				Value = obj;
				Address = address;
			}

		}
		private readonly IntPtr processHandle;
		public InjectionClient InjectionClient { get; private set; }
		public Process TargetProcess { get; }

		public IMemorySource Parent => null;
		public Il2CsRuntimeContext ParentContext => this;
		internal Il2CppTypeCache TypeCache = new();

		public Il2CsRuntimeContext(Process target)
		{
			TargetProcess = target;
			InjectionClient = new(target);
			processHandle = NativeWrapper.OpenProcess(ProcessAccessFlags.Read, inheritHandle: true, TargetProcess.Id);
		}

		public void Dispose()
		{
			NativeWrapper.CloseHandle(processHandle);
			InjectionClient.Dispose();
		}

		public ReadOnlyMemory<byte> ReadMemory(ulong address, ulong size)
		{
			byte[] buffer = new byte[size];
			if (!NativeWrapper.ReadProcessMemoryArray(processHandle, (IntPtr)address, buffer))
			{
				RuntimeError.ReadProcessMemoryReadFailed.Raise($"Failed to read memory location. GetLastError() = {NativeWrapper.LastError}");
			}
			return new(buffer);
		}

		public void WriteMemory(ulong address, ulong size, byte[] buffer)
		{
			if ((ulong)buffer.Length != size)
			{
				throw new ArgumentOutOfRangeException("Buffer length does not match size parameter;");
			}
			if (!NativeWrapper.WriteProcessMemoryArray<byte>(processHandle, (IntPtr)address, buffer))
			{
				RuntimeError.WriteProcessMemoryWriteFailed.Raise($"Failed to write memory location. GetLastError() = {NativeWrapper.LastError}");
			}
		}

		internal CachedMemoryBlock CacheMemory(ulong address, ulong size)
		{
			byte[] buffer = new byte[size];
			if (!NativeWrapper.ReadProcessMemoryArray(processHandle, (IntPtr)address, buffer))
			{
				RuntimeError.ReadProcessMemoryReadFailed.Raise($"Failed to read memory location. GetLastError() = {NativeWrapper.LastError}");
			}
			return new CachedMemoryBlock(this, address, buffer);
		}

		public static ulong GetTypeSize(Type type)
		{
			if (TypeSizes.TryGetValue(type, out int size))
			{
				return (uint)size;
			}

			if (type.IsEnum)
			{
				return GetTypeSize(type.GetEnumUnderlyingType());
			}

			if (type.IsArray)
			{
				return 8;
			}

			// pointer
			if (!type.IsValueType)
			{
				return 8;
			}

			if (type.IsAssignableTo(typeof(RuntimeObject)))
				return 8;

			throw new NotSupportedException("Unexpected type === unknown size");
		}
	}
}

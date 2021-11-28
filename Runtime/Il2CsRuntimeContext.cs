using ProcessMemoryUtilities.Managed;
using ProcessMemoryUtilities.Native;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Il2CppToolkit.Common.Errors;
using static Il2CppToolkit.Runtime.Types.Types;

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
        private readonly Dictionary<string, ulong> moduleAddresses = new();
        private readonly IntPtr processHandle;
        public Process TargetProcess { get; }

        public IMemorySource Parent => null;
        public Il2CsRuntimeContext ParentContext => this;

        public Il2CsRuntimeContext(Process target)
        {
            TargetProcess = target;
            processHandle = NativeWrapper.OpenProcess(ProcessAccessFlags.Read, inheritHandle: true, TargetProcess.Id);
        }

        public void Dispose()
        {
            NativeWrapper.CloseHandle(processHandle);
        }

        public ulong? GetMemberFieldOffset(FieldInfo field)
        {
            AddressAttribute addressAttr = field.GetCustomAttribute<AddressAttribute>(inherit: true);
            if (addressAttr != null)
            {
                ulong address = addressAttr.Address;
                if (!string.IsNullOrEmpty(addressAttr.RelativeToModule))
                {
                    address += GetModuleAddress(addressAttr.RelativeToModule);
                }
                return address;
            }

            OffsetAttribute offsetAttr = field.GetCustomAttribute<OffsetAttribute>(inherit: true);
            if (offsetAttr != null)
            {
                return offsetAttr.OffsetBytes;
            }

            DynamicOffsetAttribute dynamicOffsetAttr = field.GetCustomAttribute<DynamicOffsetAttribute>(inherit: true);
            if (dynamicOffsetAttr != null)
            {
                return null;
            }
            RuntimeError.OffsetRequired.Raise($"Field {field.Name} requires offset information");
            return null;
        }

        public ulong GetModuleAddress(string moduleName)
        {
            if (moduleAddresses.ContainsKey(moduleName))
                return moduleAddresses[moduleName];

            ProcessModule module = TargetProcess.Modules.OfType<ProcessModule>().FirstOrDefault(m => m.ModuleName == moduleName);

            if (module == null)
                throw new Exception("Unable to locate GameAssembly.dll in memory");

            moduleAddresses.Add(moduleName, (ulong)module.BaseAddress);
            return moduleAddresses[moduleName];
        }

        public ReadOnlyMemory<byte> ReadMemory(ulong address, ulong size)
        {
            byte[] buffer = new byte[size];
            if (!NativeWrapper.ReadProcessMemoryArray(processHandle, (IntPtr)address, buffer))
            {
                RuntimeError.ReadProcessMemoryReadFailed.Raise($"Failed to read memory location. GetLastError() = {NativeWrapper.LastError}");
            }
            return buffer;
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

            SizeAttribute sizeAttr = type.GetCustomAttribute<SizeAttribute>(true);
            if (sizeAttr != null)
            {
                return sizeAttr.Size;
            }

            PropertyInfo pi = type.GetProperty("NativeSize", BindingFlags.Static | BindingFlags.NonPublic);
            ulong? value = (ulong?)pi?.GetValue(type);
            if (value.HasValue)
            {
                return value.Value;
            }

            throw new NotSupportedException("Unexpected type === unknown size");
        }
    }
}

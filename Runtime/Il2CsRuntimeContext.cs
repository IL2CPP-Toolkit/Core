using ProcessMemoryUtilities.Managed;
using ProcessMemoryUtilities.Native;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Il2CppToolkit.Common.Errors;
using Il2CppToolkit.Runtime.Types;
using Il2CppToolkit.Runtime.Types.corelib;
using static Il2CppToolkit.Runtime.Types.Types;

namespace Il2CppToolkit.Runtime
{
    public class Il2CsRuntimeContext
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
        private readonly ReadProcessMemoryCache rpmCache = new();
        private readonly IntPtr processHandle;
        public Process TargetProcess { get; }
        public event EventHandler<ObjectEventArgs> ObjectCreated;
        public event EventHandler<ObjectEventArgs> ObjectLoaded;

        public Il2CsRuntimeContext(Process target)
        {
            TargetProcess = target;
            processHandle = NativeWrapper.OpenProcess(ProcessAccessFlags.Read, inheritHandle: true, TargetProcess.Id);
        }

        public ulong GetMemberFieldOffset(FieldInfo field)
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
            ErrorHandler.VerifyElseThrow(offsetAttr != null, RuntimeError.OffsetRequired, $"Field {field.Name} requires an OffsetAttribute");

            return offsetAttr.OffsetBytes;
        }

        public virtual void ReadFields(Type type, object target, ulong targetAddress)
        {
            MethodInfo readFieldsOverride = type.GetMethod(
                "ReadFields",
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public,
                null,
                CallingConventions.HasThis,
                new[]
                {
                    typeof(Il2CsRuntimeContext),
                    typeof(ulong),
                },
                null);

            if (readFieldsOverride != null)
            {
                readFieldsOverride.Invoke(target, new object[] { this, targetAddress });
                return;
            }

            do
            {
                FieldInfo[] fields =
                    type.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                foreach (FieldInfo field in fields)
                {
                    ReadField(target, targetAddress, field);
                }

                type = type.BaseType;
            } while (type != null && type != typeof(StructBase) && type != typeof(object) && type != typeof(ValueType));
            ObjectLoaded?.Invoke(this, new ObjectEventArgs(target, targetAddress));
        }

        public void ReadField(object target, ulong targetAddress, FieldInfo field)
        {
            if (field.GetCustomAttribute<IgnoreAttribute>(inherit: true) != null)
            {
                return;
            }
            ulong offset = targetAddress + GetMemberFieldOffset(field);
            byte indirection = 1;
            IndirectionAttribute indirectionAttr = field.GetCustomAttribute<IndirectionAttribute>(inherit: true);
            if (indirectionAttr != null)
            {
                indirection = indirectionAttr.Indirection;
            }

            object result = ReadValue(field.FieldType, offset, indirection);
            field.SetValue(target, result);
        }

        public T ReadValue<T>(ulong address, byte indirection = 1)
        {
            return (T)ReadValue(typeof(T), address, indirection);
        }

        public object ReadValue(Type type, ulong address, byte indirection = 1)
        {
            if (!type.IsValueType)
            {
                ++indirection;
            }
            for (; indirection > 1; --indirection)
            {
                address = ReadPointer(address);
                if (address == 0)
                {
                    return default;
                }
            }
            if (address == 0)
            {
                return default;
            }
            if (type == typeof(string))
            {
                return ReadString(address);
            }
            if (type.IsEnum)
            {
                return this.ReadPrimitive(type.GetEnumUnderlyingType(), address);
            }
            if (type.IsPrimitive)
            {
                return this.ReadPrimitive(type, address);
            }
            return ReadStruct(type, address);
        }

        private object ReadString(ulong address)
        {
            Native__String? str = ReadValue<Native__String>(address);
            return str?.Value;
        }

        public object ReadStruct(Type type, ulong address)
        {
            if (address == 0)
            {
                return null;
            }
            if (type.IsInterface || type.IsAbstract)
            {
                UnknownClass unk = (UnknownClass)ReadStruct(typeof(UnknownClass), address);

                if (unk?.ClassDefinition == null)
                    return null;

                type = LoadedTypes.GetType(unk.ClassDefinition);

                if (type == null)
                    return null;
                if (type.IsGenericType)
                {
                    Debugger.Break();
                }
            }
            if (type.IsAssignableTo(typeof(StructBase)))
            {
                object classObject = Activator.CreateInstance(type, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, null, new object[] { this, address }, null);
                ObjectCreated?.Invoke(this, new ObjectEventArgs(classObject, address));
                return classObject;
            }
            // value type
            object valueObject = Activator.CreateInstance(type);
            ObjectCreated?.Invoke(this, new ObjectEventArgs(valueObject, address));
            ReadFields(type, valueObject, address);
            return valueObject;
        }

        public static ulong GetTypeSize(Type type)
        {
            if (TypeSizes.TryGetValue(type, out int size))
            {
                return (uint)size;
            }
            // pointer
            if (!type.IsValueType)
            {
                if (!type.IsAssignableTo(typeof(StructBase)))
                {
                    // TODO: Catch if there are other cases we didn't anticipate
                    throw new NotSupportedException("Unexpected type === unknown size");
                }
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

        internal ulong GetModuleAddress(string moduleName)
        {
            if (moduleAddresses.ContainsKey(moduleName))
                return moduleAddresses[moduleName];

            ProcessModule module = TargetProcess.Modules.OfType<ProcessModule>().FirstOrDefault(m => m.ModuleName == moduleName);

            if (module == null)
                throw new Exception("Unable to locate GameAssembly.dll in memory");

            moduleAddresses.Add(moduleName, (ulong)module.BaseAddress);
            return moduleAddresses[moduleName];
        }

        internal ulong ReadPointer(ulong address)
        {
            IntPtr ptr = IntPtr.Zero;
            NativeWrapper.ReadProcessMemory(processHandle, (IntPtr)address, ref ptr);
            return (ulong)ptr;
        }

        private ulong CacheHits = 0;
        private ulong CacheMisses = 0;
        internal ReadOnlyMemory<byte> ReadMemory(ulong address, ulong size)
        {
            ReadOnlyMemory<byte>? result = rpmCache.Find(address, size);
            if (result != null)
            {
                ++CacheHits;
                return result.Value;
            }
            ++CacheMisses;
            byte[] buffer = new byte[size];
            if (!NativeWrapper.ReadProcessMemoryArray(processHandle, (IntPtr)address, buffer))
            {
                RuntimeError.ReadProcessMemoryReadFailed.Raise("Failed to read memory location");
            }
            return buffer;
        }

        internal MemoryCacheEntry CacheMemory(ulong address, ulong size)
        {
            MemoryCacheEntry result = rpmCache.FindEntry(address, size);
            if (result != null)
            {
                return result;
            }
            byte[] buffer = new byte[size];
            if (!NativeWrapper.ReadProcessMemoryArray(processHandle, (IntPtr)address, buffer))
            {
                RuntimeError.ReadProcessMemoryReadFailed.Raise("Failed to read memory location");
            }
            return rpmCache.Store(address, buffer);
        }
    }
}

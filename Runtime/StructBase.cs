using ProcessMemoryUtilities.Managed;
using ProcessMemoryUtilities.Native;
using System;
using System.Reflection;
using Il2CppToolkit.Common.Errors;
using Il2CppToolkit.Runtime.Types.Reflection;

namespace Il2CppToolkit.Runtime
{
    public abstract class StructBase
    {
        private bool m_isLoaded = false;
        private MemoryCacheEntry m_cache;

        public Il2CsRuntimeContext Context { get; set; }
        public ulong Address { get; set; }

        public virtual ClassDefinition ClassDefinition
        {
            get
            {
                return Context.ReadValue<ClassDefinition>(Address);
            }
        }

        protected virtual uint? Native__ObjectSize
        {
            get
            {
                SizeAttribute sizeAttr = GetType().GetCustomAttribute<SizeAttribute>(inherit: true);
                return sizeAttr?.Size;
            }
        }

        protected StructBase(Il2CsRuntimeContext context, ulong address)
        {
            Context = context;
            Address = address;
        }

        public T As<T>()
        {
            // avoid double-indirection used to get to this type by passing indirection=0
            T cast = (T)Context.ReadValue(typeof(T), Address, 0);
            return cast;
        }

        protected internal virtual void Load()
        {
            if (m_isLoaded)
            {
                return;
            }
            m_isLoaded = true;
            EnsureCache();
            Context.ReadFields(GetType(), this, Address);
        }

        protected virtual void EnsureCache()
        {
            if (m_cache != null || !Native__ObjectSize.HasValue || Address == 0)
            {
                return;
            }
            uint? size = Native__ObjectSize;
            IntPtr handle = NativeWrapper.OpenProcess(ProcessAccessFlags.Read, inheritHandle: true, Context.TargetProcess.Id);
            byte[] buffer = new byte[size.Value];
            if (!NativeWrapper.ReadProcessMemoryArray(handle, (IntPtr)Address, buffer))
            {
                RuntimeError.ReadProcessMemoryReadArrayFailed.Raise("Failed to read memory location");
            }
            m_cache = Context.CacheMemory(Address, size.Value);
        }
    }
}

using System.Reflection;
using Il2CppToolkit.Runtime.Types.Reflection;

namespace Il2CppToolkit.Runtime
{
    public abstract class StructBase
    {
        protected bool m_isLoaded = false;
        protected CachedMemoryBlock m_cache;

        public IMemorySource MemorySource { get; set; }
        public ulong Address { get; set; }
        internal CachedMemoryBlock Cache => m_cache;

        public virtual ClassDefinition ClassDefinition
        {
            get
            {
                return MemorySource.ReadValue<ClassDefinition>(Address);
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

        protected StructBase(IMemorySource source, ulong address)
        {
            MemorySource = source;
            Address = address;
        }

        public T As<T>()
        {
            // avoid double-indirection used to get to this type by passing indirection=0
            object value = MemorySource.ReadValue(typeof(T), Address, 0);
            if (value == null)
                return default;
            return (T)value;
        }

        public void Reload()
        {
            m_isLoaded = true;
            EnsureCache();
            MemorySource.ReadFields(GetType(), this, Address);
        }

        protected internal virtual void Load()
        {
            if (m_isLoaded)
                return;
            Reload();
        }

        protected virtual void EnsureCache()
        {
            if (m_cache != null || !Native__ObjectSize.HasValue || Address == 0)
                return;

            uint? size = Native__ObjectSize;
            if (size.Value == 0)
                return;
            m_cache = MemorySource.ParentContext.CacheMemory(Address, size.Value);
        }
    }
}

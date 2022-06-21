using System;

namespace Il2CppToolkit.Runtime.Types.corelib
{
    [TypeMapping(typeof(Nullable<>))]
    public struct Native__Nullable<T> : IRuntimeObject where T : struct
    {
        public Native__Nullable() => (Source, Address) = (default, default);
        public Native__Nullable(IMemorySource source, ulong address) => (Source, Address) = (source, address);
        public IMemorySource Source { get; }
        public ulong Address { get; }

        // private fields
        private T value => Il2CppTypeInfoLookup<Nullable<T>>.GetValue<T>(this, nameof(value));
        private bool has_value => Il2CppTypeInfoLookup<Nullable<T>>.GetValue<bool>(this, nameof(value));

        // properties
        public T Value => value;
        public bool HasValue => has_value;

#nullable enable
        public T? ToNullable()
        {
            if (HasValue)
            {
                return Value;
            }
            return default;
        }
#nullable restore
        public override string ToString()
        {
            if (HasValue)
            {
                return Value.ToString();
            }
            return null;
        }
    }
}

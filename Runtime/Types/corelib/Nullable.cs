using System;

namespace Il2CppToolkit.Runtime.Types.corelib
{
    [TypeMapping(typeof(Nullable<>))]
    public struct Native__Nullable<T>
    {
        public T Value { get; private set; }
        public bool HasValue { get; private set; }

        private void ReadFields(IMemorySource source, ulong address)
        {
            ReadOnlyMemory<byte> hasValue = source.ReadMemory(address + Il2CsRuntimeContext.GetTypeSize(typeof(T)), 1);
            HasValue = hasValue.ToBoolean();
            if (!HasValue)
            {
                return;
            }

            Value = source.ReadValue<T>(address);
        }

#nullable enable
        public T? ToNullable()
        {
            if (HasValue)
            {
                return Value;
            }
            return default(T?);
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

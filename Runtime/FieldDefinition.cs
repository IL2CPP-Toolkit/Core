using System;
using System.Collections.Generic;

namespace Il2CppToolkit.Runtime
{
    public interface IClassInterop
    {
        public IMemorySource Source { get; }
        public ulong Address { get; }
    }

    public class FieldDefinition<TClass, TValue>
        where TClass : IClassInterop
    {
        private ulong __offset;
        public FieldDefinition(ulong offset)
        {
            __offset = offset;
        }
        public TValue GetValue(TClass obj)
        {
            return obj.Source.ReadValue<TValue>(obj.Address + __offset);
        }
    }

    public class StaticFieldDefinition<TValue>
    {
        private string __moduleName;
        private ulong __clsOffset;
        private ulong __offset;
        public StaticFieldDefinition(string moduleName, ulong clsOffset, ulong offset)
        {
            __moduleName = moduleName;
            __clsOffset = clsOffset;
            __offset = offset;
        }
        public TValue GetValue(IMemorySource source)
        {
            ulong modOffset = source.ParentContext.GetModuleAddress(__moduleName);
            // var classDef = source.ReadValue<Il2CppToolkit.Runtime.Types.Reflection.ClassDefinition>(modOffset + __clsOffset);
            ulong staticFields = source.ReadPointer(source.ReadPointer(modOffset + __clsOffset) + 0xB8);
            return source.ReadValue<TValue>(staticFields + __offset);
        }
    }
}
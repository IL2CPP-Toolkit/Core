using System;

namespace Il2CppToolkit.Runtime
{
    public interface IMemorySource
    {
        IMemorySource Parent { get; }
        Il2CsRuntimeContext ParentContext { get; }
        ReadOnlyMemory<byte> ReadMemory(ulong address, ulong size);
    }
}
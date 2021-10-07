
namespace Il2CppToolkit.Runtime.Types.Reflection
{
    [Size(80)]
    public class NativeMethodInfo : StructBase
    {
        public NativeMethodInfo(IMemorySource source, ulong address) : base(source, address)
        {
        }

        [Offset(24)]
#pragma warning disable 649
        private ClassDefinition m_klass;
#pragma warning restore 649

        public ClassDefinition DeclaringClass { get { Load(); return m_klass; } }

    }
}

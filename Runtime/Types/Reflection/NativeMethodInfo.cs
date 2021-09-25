using Il2CppToolkit.Core;

namespace Il2CppToolkit.Runtime.Types.Reflection
{
	[Size(80)]
	public class NativeMethodInfo : StructBase
	{
		public NativeMethodInfo(Il2CsRuntimeContext context, ulong address) : base(context, address)
		{
		}

		[Offset(24)]
		[Indirection(2)]
#pragma warning disable 649
		private ClassDefinition m_klass;
#pragma warning restore 649

		public ClassDefinition DeclaringClass { get { Load(); return m_klass; } }

	}
}

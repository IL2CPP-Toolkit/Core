using System.Runtime.CompilerServices;

namespace Il2CppToolkit.Runtime
{
	public class StaticFieldMember<TClass, TValue>
		where TClass : IRuntimeObject
	{
		private readonly string Name;
		private readonly byte Indirection;
		public StaticFieldMember([CallerMemberName] string name = "", byte indirection = 1)
		{
			Name = name;
			Indirection = indirection;
		}
		public TValue GetValue(IMemorySource source) => Il2CppTypeInfoLookup<TClass>.GetStaticValue<TValue>(source.ParentContext, Name, Indirection);
	}
}
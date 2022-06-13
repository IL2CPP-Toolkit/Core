using Il2CppToolkit.Injection.Client;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Il2CppToolkit.Runtime
{
	internal class Il2CppTypeInfoLookup<TClass>
		where TClass : IRuntimeObject
	{
		private static GetTypeInfoResponse s_typeInfo;
		public static Il2CppTypeInfo GetTypeInfo(InjectionClient client)
		{
			s_typeInfo ??= client.Il2Cpp.GetTypeInfo(new() { Klass = new() { Name = typeof(TClass).FullName } });
			return s_typeInfo.TypeInfo;
		}
	}

	public class FieldMember<TClass, TValue>
		where TClass : IRuntimeObject
	{
		private readonly string Name;
		private readonly byte Indirection;
		public FieldMember([CallerMemberName] string name = "", byte indirection = 1)
		{
			Name = name;
			Indirection = indirection;
		}
		public TValue GetValue(IRuntimeObject obj)
		{
			Il2CppTypeInfo typeInfo = Il2CppTypeInfoLookup<TClass>.GetTypeInfo(obj.Source.ParentContext.InjectionClient);
			Il2CppField fld = typeInfo.Fields.First(fld => fld.Name == Name);
			return obj.Source.ReadValue<TValue>(obj.Address + fld.Offset, Indirection);
		}
	}

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
		public TValue GetValue(IMemorySource source)
		{
			Il2CppTypeInfo typeInfo = Il2CppTypeInfoLookup<TClass>.GetTypeInfo(source.ParentContext.InjectionClient);
			Il2CppField fld = typeInfo.Fields.First(fld => fld.Name == Name);
			return source.ReadValue<TValue>(typeInfo.StaticFieldsAddress + fld.Offset, Indirection);
		}
	}
}
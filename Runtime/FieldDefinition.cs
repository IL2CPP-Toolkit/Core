using Il2CppToolkit.Injection.Client;
using System;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Il2CppToolkit.Runtime
{
	public class Il2CppTypeInfoLookup<TClass>
		where TClass : IRuntimeObject
	{
		private static GetTypeInfoResponse s_typeInfo;
		public static Il2CppTypeInfo GetTypeInfo(InjectionClient client)
		{
			GetTypeInfoRequest req = new() { Klass = new() { Name = $"{typeof(TClass).Namespace}.{typeof(TClass).Name}".TrimStart('.') } };
			s_typeInfo ??= client.Il2Cpp.GetTypeInfo(req);
			return s_typeInfo.TypeInfo;
		}

		public static TValue GetValue<TValue>(IRuntimeObject obj, string name, byte indirection = 1)
		{
			Il2CppTypeInfo typeInfo = GetTypeInfo(obj.Source.ParentContext.InjectionClient);
			Il2CppField fld = typeInfo.Fields.First(fld => fld.Name == name);
			return obj.Source.ReadValue<TValue>(obj.Address + fld.Offset, indirection);
		}

		public static TValue GetStaticValue<TValue>(Il2CsRuntimeContext context, string name, byte indirection = 1)
		{
			Il2CppTypeInfo typeInfo = GetTypeInfo(context.InjectionClient);
			Il2CppField fld = typeInfo.Fields.First(fld => fld.Name == name);
			return context.ReadValue<TValue>(typeInfo.StaticFieldsAddress + fld.Offset, indirection);
		}
	}

	[Obsolete("Use Il2CppTypeInfoLookup<TClass>.GetValue(IRuntimeObject, string, byte)")]
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
		public TValue GetValue(IRuntimeObject obj) => Il2CppTypeInfoLookup<TClass>.GetValue<TValue>(obj, Name, Indirection);
	}

	//[Obsolete("Use Il2CppTypeInfoLookup<TClass>.GetStaticValue(Il2CsRuntimeContext, string, byte)")]
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
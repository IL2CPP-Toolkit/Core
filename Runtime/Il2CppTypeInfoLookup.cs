using Il2CppToolkit.Injection.Client;
using System;
using System.Diagnostics;
using System.Linq;

namespace Il2CppToolkit.Runtime
{
	public class Il2CppTypeInfoLookup<TClass>
	{
		private static GetTypeInfoResponse s_typeInfo;
		public static Il2CppTypeInfo GetTypeInfo(InjectionClient client)
		{
			s_typeInfo ??= client.Il2Cpp.GetTypeInfo(CreateRequest(), null, DateTime.MaxValue, default);
			return s_typeInfo.TypeInfo;
		}

		private static GetTypeInfoRequest CreateRequest()
		{
			return new() { Klass = new() { Name = GetTypeName(typeof(TClass), false), Namespaze = typeof(TClass).Namespace } };
		}

		private static string GetTypeName(Type type, bool includeFirst = true)
		{
			string typeName = includeFirst ? type.Namespace : "";
			if (!string.IsNullOrEmpty(typeName))
				typeName += ".";
			typeName += type.Name;

			if (type.IsConstructedGenericType)
			{
				typeName = typeName.Substring(0, typeName.Length - 2);
				typeName += "<";
				typeName += string.Join(",", type.GenericTypeArguments.Select(arg => GetTypeName(arg)));
				typeName += ">";
			}
			Trace.TraceInformation(typeName);
			return typeName;
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
}
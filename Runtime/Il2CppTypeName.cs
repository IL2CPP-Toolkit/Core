using System;
using System.Linq;
using Il2CppToolkit.Injection.Client;

namespace Il2CppToolkit.Runtime
{
	public static class Il2CppTypeName
	{
		public static ClassId GetKlass(Type type)
		{
			return new() { Name = GetTypeName(type, false), Namespaze = type.Namespace };
		}
		public static string GetTypeName(Type type, bool includeFirst = true)
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
			return typeName;
		}
	}
	public static class Il2CppTypeName<TClass>
	{
		public static ClassId klass = Il2CppTypeName.GetKlass(typeof(TClass));
	}
}
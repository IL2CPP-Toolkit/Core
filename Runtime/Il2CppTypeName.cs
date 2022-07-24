using System;
using System.Linq;
using Il2CppToolkit.Injection.Client;

namespace Il2CppToolkit.Runtime
{
	public static class Il2CppTypeName
	{
		public static ClassId GetKlass(Type type)
		{
			ClassId klass = new() { Name = GetTypeName(type, false), Namespaze = type.Namespace };
			Type declaringType = type;
			ClassId currentKlass = klass;
			while ((declaringType = declaringType.DeclaringType) != null)
			{
				currentKlass = currentKlass.DeclaringType = GetKlass(declaringType);
			}
			return klass;
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

		public static string GetTypeName(ClassId type)
		{
			string typeName = string.Empty;
			if (type.DeclaringType != null)
			{
				typeName = GetTypeName(type.DeclaringType);
				typeName += "+";
			}
			if (!string.IsNullOrEmpty(type.Namespaze))
			{
				typeName += type.Namespaze;
				typeName += ".";
			}
			typeName += type.Name;
			return typeName;
		}
	}
	public static class Il2CppTypeName<TClass>
	{
		public static ClassId klass = Il2CppTypeName.GetKlass(typeof(TClass));
	}
}
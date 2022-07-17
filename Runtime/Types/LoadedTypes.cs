using System;
using System.Collections.Generic;
using System.Reflection;
using Il2CppToolkit.Runtime.Types.Reflection;

namespace Il2CppToolkit.Runtime.Types
{
	public static class LoadedTypes
	{
		private static readonly Dictionary<string, Type> s_nameToType = new();

		public static Type GetType(ClassDefinition classDef)
		{
			return s_nameToType.TryGetValue(classDef.FullName, out Type retVal) ? retVal : null;
		}

		static LoadedTypes()
		{
			foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
			{
				if (asm.GetCustomAttribute<GeneratedAttribute>() == null) continue;

				foreach (Type type in asm.GetTypes())
				{
					GeneratedAttribute attr = type.GetCustomAttribute<GeneratedAttribute>();
					if (attr == null) continue;

					s_nameToType.TryAdd(type.FullName, type);
				}
			}
		}
	}
}

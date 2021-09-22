using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using IL2CS.Core;
using IL2CS.Runtime.Types.Reflection;

namespace IL2CS.Runtime.Types
{
	public static class LoadedTypes
	{
		private static readonly Dictionary<ulong, Type> s_tokenToType = new();

		public static Type GetType(ClassDefinition classDef)
		{
			ulong token = Utilities.GetTypeTag(classDef.Image.TypeStart, classDef.Token);
			return s_tokenToType.TryGetValue(token, out Type retVal) ? retVal : null;
		}

		static LoadedTypes()
		{
			foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
			{
				if (asm.GetCustomAttribute<GeneratedAttribute>() == null) continue;

				foreach (Type type in asm.GetTypes())
				{
					TagAttribute attr = type.GetCustomAttribute<TagAttribute>();
					if (attr == null) continue;

					DebugHelpers.Assert(!s_tokenToType.ContainsKey(attr.Tag), "Duplicate type by token");
					s_tokenToType.Add(attr.Tag, type);
				}
			}
		}
	}
}

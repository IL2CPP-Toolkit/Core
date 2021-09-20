using System;
using System.Collections.Generic;

namespace Il2CppToolkit.Common
{
	public class UniqueName
	{
		private readonly HashSet<string> uniqueNamesHash = new HashSet<string>(StringComparer.Ordinal);

		public string Get(string name)
		{
			string uniqueName = name;
			int i = 1;
			while (!uniqueNamesHash.Add(uniqueName))
			{
				uniqueName = $"{name}_{i++}";
			}
			return uniqueName;
		}
	}
}
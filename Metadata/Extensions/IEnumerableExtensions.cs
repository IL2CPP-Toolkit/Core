using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Il2CppToolkit.Model
{
	static class IEnumerableExtensions
	{
		public static IEnumerable<(int index, T value)> WithIndexes<T>(this IEnumerable<T> target)
		{
			int index = 0;
			foreach (var entry in target)
			{
				yield return (index, entry);
			}
		}
	}
}

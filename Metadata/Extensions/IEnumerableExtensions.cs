using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Il2CppToolkit.Model
{
	static class IEnumerableExtensions
	{
		public static IEnumerable<T> Range<T>(this IEnumerable<T> target, int start, int count)
		{
			return target.Skip(start).Take(count);
		}

		public static IEnumerable<(int index, T value)> RangeWithIndexes<T>(this IEnumerable<T> target, int start, int count)
		{
			return target.Range(start, count).Select((value, idx) => (start + idx, value));
		}

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

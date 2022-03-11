using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System
{
#if NET472
	public static class Net50Extensions
	{
		public static bool IsAssignableTo(this Type type, Type assignableTo)
		{
			return assignableTo.IsAssignableFrom(type);
		}

		public static bool TryAdd<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, TValue value)
		{
			if (dict.ContainsKey(key))
				return false;
			dict.Add(key, value);
			return true;
		}

		public static bool TryDequeue<T>(this Queue<T> queue, out T value)
		{
			if (queue.Count == 0)
			{
				value = default;
				return false;
			}
			value = queue.Dequeue();
			return true;
		}
	}
#endif
}

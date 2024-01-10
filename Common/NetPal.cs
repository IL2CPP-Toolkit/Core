using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Il2CppToolkit.Metadata")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Il2CppToolkit.Target.TSDef")]

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


namespace System.Diagnostics.CodeAnalysis
{
#if NET472

	//
	// Summary:
	//     Specifies that when a method returns System.Diagnostics.CodeAnalysis.NotNullWhenAttribute.ReturnValue,
	//     the parameter will not be null even if the corresponding type allows it.
	[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
	public internal class NotNullWhenAttribute : Attribute
	{
		//
		// Summary:
		//     Gets the return value condition.
		public bool ReturnValue { get; }

		//
		// Summary:
		//     Initializes the attribute with the specified return value condition.
		//
		// Parameters:
		//   returnValue:
		//     The return value condition. If the method returns this value, the associated
		//     parameter will not be null.
		public NotNullWhenAttribute(bool returnValue)
		{
			ReturnValue = returnValue;
		}
	}
#endif
}
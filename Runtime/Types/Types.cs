using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace IL2CS.Runtime.Types
{
	public static class Types
	{
		public static bool TryGetType(string typeName, out Type mappedType)
		{
			if (NativeMapping.TryGetValue(typeName, out mappedType))
			{
				return true;
			}
			if (typeName.StartsWith("System."))
			{
				Trace.WriteLine($"Omitting type '{typeName}'");
				mappedType = null;
				return true;
			}
			return false;
		}
		private static readonly Dictionary<string, Type> NativeMapping = new();
		static Types()
		{
			NativeMapping.Add(typeof(ValueType).FullName, typeof(ValueType));
			foreach (var (mapFrom, mapTo) in GetTypesWithMappingAttribute(typeof(Types).Assembly))
			{
				NativeMapping.Add(mapFrom.FullName, mapTo);
			}
		}
		static IEnumerable<(Type, Type)> GetTypesWithMappingAttribute(Assembly assembly)
		{
			foreach (Type type in assembly.GetTypes())
			{
				TypeMappingAttribute tma = type.GetCustomAttribute<TypeMappingAttribute>(true);
				if (tma != null)
				{
					yield return (tma.Type, type);
				}
			}
		}

		public static readonly Dictionary<Type, int> TypeSizes = new()
		{
			{typeof(void), 0},
			{typeof(bool), sizeof(bool)},
			{typeof(char), sizeof(char)},
			{typeof(sbyte), sizeof(sbyte)},
			{typeof(byte), sizeof(byte)},
			{typeof(short), sizeof(short)},
			{typeof(ushort), sizeof(ushort)},
			{typeof(int), sizeof(int)},
			{typeof(uint), sizeof(uint)},
			{typeof(long), sizeof(long)},
			{typeof(ulong), sizeof(ulong)},
			{typeof(float), sizeof(float)},
			{typeof(double), sizeof(double)},
			{typeof(string), 8},
			{typeof(IntPtr), 8},
			{typeof(UIntPtr), 8},
			{typeof(object), 8},
		};

	}
}

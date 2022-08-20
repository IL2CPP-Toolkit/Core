using Il2CppToolkit.Common.Errors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Il2CppToolkit.Runtime.Types
{
	public static class TypeSystem
	{
		public static bool TryGetSubstituteType(string typeName, out Type mappedType, Type declaringType = null)
		{
			if (NativeMapping.TryGetValue(typeName, out mappedType))
			{
				return true;
			}
			Type builtinType = declaringType != null ? declaringType.GetNestedType(typeName) : Type.GetType(typeName, false);
			if (builtinType != null)
			{
				if (NativeFactoryMapping.TryGetValue(builtinType, out _) || builtinType.IsInterface)
				{
					mappedType = builtinType;
					return true;
				}
			}
			if (typeName.StartsWith("System."))
			{
				mappedType = null;
				return true;
			}
			return false;
		}

		public static bool TryGetTypeFactory(Type type, out ITypeFactory typeFactory)
		{
			if (!NativeFactoryMapping.TryGetValue(type, out Type typeFactoryType)
				&& (!type.IsConstructedGenericType || !NativeFactoryMapping.TryGetValue(type.GetGenericTypeDefinition(), out typeFactoryType)))
			{
				typeFactory = null;
				return false;
			}

			if (NativeFactoryInstances.TryGetValue(type, out typeFactory))
			{
				return true;
			}

			ErrorHandler.VerifyElseThrow(
				typeFactoryType.IsAssignableTo(typeof(ITypeFactory)),
				RuntimeError.TypeFactoryImplementationMissing,
				$"Class marked with [TypeFactoryAttribute] must extend ITypeFactory: '{typeFactoryType.FullName}'");
			if (type.IsGenericType)
			{
				ErrorHandler.VerifyElseThrow(
					typeFactoryType.IsGenericTypeDefinition,
					RuntimeError.GenericFactoryRequired,
					"A generic type must have a generic factory");
				typeFactoryType = typeFactoryType.MakeGenericType(type.GenericTypeArguments);
			}

			typeFactory = Activator.CreateInstance(typeFactoryType) as ITypeFactory;
			NativeFactoryInstances.TryAdd(type, typeFactory);
			return true;
		}

		private static readonly Dictionary<Type, Type> NativeFactoryMapping = new();
		private static readonly Dictionary<Type, ITypeFactory> NativeFactoryInstances = new();
		private static readonly Dictionary<string, Type> NativeMapping = new();

		static TypeSystem()
		{
			NativeMapping.Add(typeof(ValueType).FullName, typeof(ValueType));
			foreach (var (mapFrom, mapTo) in GetTypesWithMappingAttribute(typeof(TypeSystem).Assembly))
			{
				NativeMapping.Add(mapFrom.FullName, mapTo);
			}
			foreach (var (attr, type) in GetTypesWithFactoryAttribute(typeof(TypeSystem).Assembly))
			{
				NativeFactoryMapping.Add(attr.Type, type);
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
		static IEnumerable<(TypeFactoryAttribute, Type)> GetTypesWithFactoryAttribute(Assembly assembly)
		{
			foreach (Type type in assembly.GetTypes())
			{
				TypeFactoryAttribute tma = type.GetCustomAttribute<TypeFactoryAttribute>(true);
				if (tma != null)
				{
					yield return (tma, type);
				}
			}
		}

		public static readonly Dictionary<Type, int> TypeSizes = new()
		{
			{ typeof(void), 0 },
			{ typeof(bool), sizeof(byte) },
			{ typeof(char), sizeof(char) },
			{ typeof(sbyte), sizeof(sbyte) },
			{ typeof(byte), sizeof(byte) },
			{ typeof(short), sizeof(short) },
			{ typeof(ushort), sizeof(ushort) },
			{ typeof(int), sizeof(int) },
			{ typeof(uint), sizeof(uint) },
			{ typeof(long), sizeof(long) },
			{ typeof(ulong), sizeof(ulong) },
			{ typeof(float), sizeof(float) },
			{ typeof(double), sizeof(double) },
			{ typeof(string), 8 },
			{ typeof(IntPtr), 8 },
			{ typeof(UIntPtr), 8 },
			{ typeof(object), 8 },
		};

	}
}

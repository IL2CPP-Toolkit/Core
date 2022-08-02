using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Il2CppToolkit.Runtime.Types;
using Il2CppToolkit.Runtime.Types.corelib;
using Il2CppToolkit.Runtime.Types.corelib.Collections.Generic;

namespace Il2CppToolkit.Runtime
{
	public class MemoryAccessEventArgs : EventArgs
	{
		public ulong Address { get; }
		public Type Type { get; }
		public MemoryAccessEventArgs(Type type, ulong address) : base() => (Address, Type) = (address, type);
	}
	public class MemoryAccessErrorEventArgs : MemoryAccessEventArgs
	{
		public Exception Exception { get; }
		public MemoryAccessErrorEventArgs(Type type, ulong address, Exception ex) : base(type, address) => Exception = ex;

	}
	public static class MemorySourceExtensions
	{
		private static Assembly ThisAsm;
		public static event EventHandler<MemoryAccessEventArgs> ObjectReadFromMemory;
		public static event EventHandler<MemoryAccessErrorEventArgs> ObjectReadError;

		private class ConvertPrimitive
		{
			public Func<IMemorySource, ulong, object> ReadFn;
			public Action<IMemorySource, ulong, object> WriteFn;
		}

		private static readonly Dictionary<Type, ConvertPrimitive> s_implMap = new();
		static MemorySourceExtensions()
		{
			ThisAsm = typeof(MemorySourceExtensions).Assembly;
			s_implMap.Add(typeof(Char), new()
			{
				ReadFn = (context, address) => context.ReadMemory(address, sizeof(Char)).ToChar(),
				WriteFn = (context, address, value) => context.ParentContext.WriteMemory(address, sizeof(Char), BitConverter.GetBytes((char)value)),
			});
			s_implMap.Add(typeof(Boolean), new()
			{
				ReadFn = (context, address) => context.ReadMemory(address, sizeof(Boolean)).ToBoolean(),
				WriteFn = (context, address, value) => context.ParentContext.WriteMemory(address, sizeof(Boolean), BitConverter.GetBytes((Boolean)value)),
			});
			s_implMap.Add(typeof(Double), new()
			{
				ReadFn = (context, address) => context.ReadMemory(address, sizeof(Double)).ToDouble(),
				WriteFn = (context, address, value) => context.ParentContext.WriteMemory(address, sizeof(Double), BitConverter.GetBytes((Double)value)),
			});
			s_implMap.Add(typeof(Single), new()
			{
				ReadFn = (context, address) => context.ReadMemory(address, sizeof(Single)).ToSingle(),
				WriteFn = (context, address, value) => context.ParentContext.WriteMemory(address, sizeof(Single), BitConverter.GetBytes((Single)value)),
			});
			s_implMap.Add(typeof(Int16), new()
			{
				ReadFn = (context, address) => context.ReadMemory(address, sizeof(Int16)).ToInt16(),
				WriteFn = (context, address, value) => context.ParentContext.WriteMemory(address, sizeof(Int16), BitConverter.GetBytes((Int16)value)),
			});
			s_implMap.Add(typeof(Int32), new()
			{
				ReadFn = (context, address) => context.ReadMemory(address, sizeof(Int32)).ToInt32(),
				WriteFn = (context, address, value) => context.ParentContext.WriteMemory(address, sizeof(Int32), BitConverter.GetBytes((Int32)value)),
			});
			s_implMap.Add(typeof(Int64), new()
			{
				ReadFn = (context, address) => context.ReadMemory(address, sizeof(Int64)).ToInt64(),
				WriteFn = (context, address, value) => context.ParentContext.WriteMemory(address, sizeof(Int64), BitConverter.GetBytes((Int64)value)),
			});
			s_implMap.Add(typeof(UInt16), new()
			{
				ReadFn = (context, address) => context.ReadMemory(address, sizeof(UInt16)).ToUInt16(),
				WriteFn = (context, address, value) => context.ParentContext.WriteMemory(address, sizeof(UInt16), BitConverter.GetBytes((UInt16)value)),
			});
			s_implMap.Add(typeof(UInt32), new()
			{
				ReadFn = (context, address) => context.ReadMemory(address, sizeof(UInt32)).ToUInt32(),
				WriteFn = (context, address, value) => context.ParentContext.WriteMemory(address, sizeof(UInt32), BitConverter.GetBytes((UInt32)value)),
			});
			s_implMap.Add(typeof(UInt64), new()
			{
				ReadFn = (context, address) => context.ReadMemory(address, sizeof(UInt64)).ToUInt64(),
				WriteFn = (context, address, value) => context.ParentContext.WriteMemory(address, sizeof(UInt64), BitConverter.GetBytes((UInt64)value)),
			});
			s_implMap.Add(typeof(IntPtr), new()
			{
				ReadFn = (context, address) => context.ReadMemory(address, sizeof(Int64)).ToIntPtr(),
				WriteFn = (context, address, value) => context.ParentContext.WriteMemory(address, sizeof(Int64), BitConverter.GetBytes((Int64)value)),
			});
			s_implMap.Add(typeof(UIntPtr), new()
			{
				ReadFn = (context, address) => context.ReadMemory(address, sizeof(UInt64)).ToUIntPtr(),
				WriteFn = (context, address, value) => context.ParentContext.WriteMemory(address, sizeof(UInt64), BitConverter.GetBytes((UInt64)value)),
			});
		}


		public static T ReadValue<T>(this IMemorySource source, ulong address, byte indirection = 1)
		{
			object result = ReadValue(source, typeof(T), address, indirection);
			return (T)result;
		}

		public static object ReadValue(this IMemorySource source, Type type, ulong address, byte indirection = 1)
		{
			try
			{
				ObjectReadFromMemory?.Invoke(source, new(type, address));
				if (!type.IsValueType)
				{
					++indirection;
				}
				for (; indirection > 1; --indirection)
				{
					address = ReadPointer(source, address);
					if (address == 0)
						break;
				}
				if (address == 0 && !type.IsAssignableTo(typeof(INullConstructable)))
				{
					return default;
				}
				if (TypeSystem.TryGetTypeFactory(type, out ITypeFactory factory))
				{
					return factory.ReadValue(source, address);
				}
				if (type.IsEnum)
				{
					return ReadPrimitive(source, type.GetEnumUnderlyingType(), address);
				}
				if (type.IsPrimitive)
				{
					return ReadPrimitive(source, type, address);
				}
				return ReadStruct(source, type, address);
			}
			catch (Exception ex)
			{
				ObjectReadError?.Invoke(source, new(type, address, ex));
				throw;
			}
		}

		public static ulong ReadPointer(this IMemorySource source, ulong address)
		{
			return ReadPrimitive<ulong>(source, address);
		}

		private static object ReadStruct(this IMemorySource source, Type type, ulong address)
		{
			if (address == 0 && !type.IsAssignableTo(typeof(INullConstructable)))
			{
				return null;
			}
			if (type.IsArray)
			{
				dynamic array = Activator.CreateInstance(typeof(Native__Array<>).MakeGenericType(type.GetElementType()), new object[] { (IMemorySource)source, address });
				object result = array.Array;
				return result;
			}

			// if (type.IsInterface || type.IsAbstract || type == typeof(Object))
			if (type.Assembly != ThisAsm && !type.IsValueType)
			{
				Type originalType = type;
				UnknownClass unk = (UnknownClass)ReadStruct(source, typeof(UnknownClass), address);

				if (unk?.ClassDefinition == null)
					return originalType == typeof(object) ? unk : null;

				type = LoadedTypes.GetType(unk.ClassDefinition);

				if (type == null)
					return originalType == typeof(object) ? unk : null;

				// make sure we always fetch metadata for types at time of hydration
				// this is the one time we can be 100% sure of the concrete type that
				// obj->klass points to, and we don't get another chance to resolve
				// this type deterministically (nested types can't be looked up by name)
				if (!Il2CppTypeCache.HasType(type))
				{
					Il2CppTypeCache.GetTypeInfo(source.ParentContext, type, unk.ClassDefinition.Address);
				}

				if (type.IsGenericType && type.ContainsGenericParameters)
				{
					if(originalType.IsGenericType && originalType.ContainsGenericParameters)
					{
						return originalType == typeof(object) ? unk : null;
					}
					else // if (type.GetGenericTypeDefinition() == originalType)
					{
						type = originalType;
					}
				}
			}
			if (type.IsAssignableTo(typeof(IRuntimeObject)))
			{
				object classObject = Activator.CreateInstance(type, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, null, new object[] { (IMemorySource)source, address }, null);
				return classObject;
			}
			if (type.GetConstructors().Length != 0 && type.GetConstructor(Array.Empty<Type>()) == null)
			{
				return null;
			}
			return null;
		}

		private static object ReadPrimitive(this IMemorySource context, Type type, ulong address)
		{
			if (s_implMap.TryGetValue(type, out var impl))
			{
				return impl.ReadFn(context, address);
			}
			throw new ArgumentException($"Type '{type.FullName}' is not a valid primitive type");
		}

		private static T ReadPrimitive<T>(this IMemorySource context, ulong address)
		{
			if (s_implMap.TryGetValue(typeof(T), out var impl))
			{
				return (T)impl.ReadFn(context, address);
			}
			throw new ArgumentException($"Type '{typeof(T).FullName}' is not a valid primitive type");
		}
	}
}

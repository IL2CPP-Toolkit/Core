using Il2CppToolkit.Common.Errors;
using Il2CppToolkit.Injection.Client;
using Il2CppToolkit.Runtime.Types.Reflection;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using static Il2CppToolkit.Injection.Client.Value;

namespace Il2CppToolkit.Runtime
{
	public class Il2CppTypeCache
	{
		private readonly ConcurrentDictionary<ulong, Il2CppTypeInfo> TypeInfoByAddr = new();
		private readonly ConcurrentDictionary<Type, Il2CppTypeInfo> TypeInfoByType = new();

		public static bool HasType(Il2CsRuntimeContext runtime, Type managedType)
		{
			return runtime.TypeCache.TypeInfoByType.TryGetValue(managedType, out _);
		}

		public static bool TryGetOrLoadTypeInfoCore(Il2CsRuntimeContext runtime, Type managedType, ulong classAddr, out Il2CppTypeInfo result)
		{
			Il2CppTypeCache typeCache = runtime.TypeCache;
			if (managedType == null)
				throw new ArgumentNullException(nameof(managedType));

			result = typeCache.TypeInfoByType.GetOrAdd(managedType, (Type managedType) =>
				classAddr > 0
					? runtime.InjectionClient.Il2Cpp.GetTypeInfo(new() { Address = classAddr }).TypeInfo
					: runtime.InjectionClient.Il2Cpp.GetTypeInfo(new() { Klass = Il2CppTypeName.GetKlass(managedType) }).TypeInfo
				);

			if (result == null)
				return false;

			return typeCache.TypeInfoByAddr.TryAdd(result.KlassId.Address, result);
		}

		public static Il2CppTypeInfo GetTypeInfo(Il2CsRuntimeContext runtime, Type managedType, ulong classAddr = 0)
		{
			if (managedType == null)
				throw new ArgumentNullException(nameof(managedType));

			if (!TryGetOrLoadTypeInfoCore(runtime, managedType, classAddr, out Il2CppTypeInfo result) || result == null)
				return result;

			if (managedType.IsValueType)
				return result;

			Type baseType = managedType;
			ClassDefinition clsDef = null;
			if (classAddr > 0)
				clsDef = new(runtime, classAddr);
			while ((baseType = baseType.BaseType) != null && baseType.GetCustomAttribute<GeneratedAttribute>() != null)
			{
				clsDef = clsDef?.Base;
				if (!TryGetOrLoadTypeInfoCore(runtime, baseType, clsDef?.Address ?? 0, out _))
					break;
			}

			return result;
		}
	}

	public class Il2CppTypeInfoLookup<TClass>
	{
		public static TValue FromValue<TValue>(IRuntimeObject obj, Value returnValue)
		{
			return returnValue.ValueCase switch
			{
				ValueOneofCase.Bit => (TValue)(object)returnValue.Bit,
				ValueOneofCase.Double => (TValue)(object)returnValue.Double,
				ValueOneofCase.Float => (TValue)(object)returnValue.Float,
				ValueOneofCase.Int32 => (TValue)(object)returnValue.Int32,
				ValueOneofCase.Int64 => (TValue)(object)returnValue.Int64,
				ValueOneofCase.Obj => HydrateObject<TValue>(obj.Source.ParentContext, returnValue.Obj),
				ValueOneofCase.Str => (TValue)(object)returnValue.Str,
				ValueOneofCase.Uint32 => (TValue)(object)returnValue.Uint32,
				ValueOneofCase.Uint64 => (TValue)(object)returnValue.Uint64,
				_ => default,
			};
		}

		public static Value ValueFrom(object value)
		{
			if (value == null)
				return new Value() { Obj = new Il2CppObject() { Address = 0 } };

			return value switch
			{
				NullableArg nullable => ValueFromNullable(nullable),
				double d => new Value() { Double = d },
				float f => new Value() { Float = f },
				int i4 => new Value() { Int32 = i4 },
				uint u4 => new Value() { Uint32 = u4 },
				ulong u8 => new Value() { Uint64 = u8 },
				long i8 => new Value() { Int64 = i8 },
				bool bit => new Value() { Bit = bit },
				string str => new Value() { Str = str },
				IRuntimeObject obj => new Value() { Obj = new Il2CppObject() { Address = obj.Address } },
				_ => throw new NotSupportedException($"Argument type of '{value.GetType().FullName}' is not supported")
			};
		}

		private static Value ValueFromNullable(NullableArg nullable)
		{
			return nullable switch
			{
				NullableArg<double> arg => new Value() { Double = arg.TypedValue, NullState = nullable.HasValue ? NullableState.HasValue : NullableState.IsNull },
				NullableArg<float> arg => new Value() { Float = arg.TypedValue, NullState = nullable.HasValue ? NullableState.HasValue : NullableState.IsNull },
				NullableArg<int> arg => new Value() { Int32 = arg.TypedValue, NullState = nullable.HasValue ? NullableState.HasValue : NullableState.IsNull },
				NullableArg<uint> arg => new Value() { Uint32 = arg.TypedValue, NullState = nullable.HasValue ? NullableState.HasValue : NullableState.IsNull },
				NullableArg<ulong> arg => new Value() { Uint64 = arg.TypedValue, NullState = nullable.HasValue ? NullableState.HasValue : NullableState.IsNull },
				NullableArg<long> arg => new Value() { Int64 = arg.TypedValue, NullState = nullable.HasValue ? NullableState.HasValue : NullableState.IsNull },
				NullableArg<bool> arg => new Value() { Bit = arg.TypedValue, NullState = nullable.HasValue ? NullableState.HasValue : NullableState.IsNull },
				_ => throw new NotSupportedException($"Argument type of '{nullable.GetType().FullName}' is not supported")
			};
		}

		public static Value CallMethodCore(Il2CsRuntimeContext context, IRuntimeObject obj, [CallerMemberName] string name = "", object[] arguments = null)
		{
			if (arguments == null)
				throw new ArgumentNullException(nameof(arguments));

			CallMethodRequest req = new()
			{
				MethodName = name,
				Klass = Il2CppTypeName<TClass>.klass
			};
			if (obj != null)
				req.Instance = new() { Address = obj.Address };

			req.Arguments.AddRange(arguments.Select(ValueFrom));
			CallMethodResponse response = context.InjectionClient.Il2Cpp.CallMethod(req);
			return response.ReturnValue;
		}

		public static void CallMethod(IRuntimeObject obj, [CallerMemberName] string name = "", object[] arguments = null)
		{
			CallMethodCore(obj.Source.ParentContext, obj, name, arguments);
		}

		public static TValue CallMethod<TValue>(IRuntimeObject obj, [CallerMemberName] string name = "", object[] arguments = null)
		{
			Value returnValue = CallMethodCore(obj.Source.ParentContext, obj, name, arguments);

			if (returnValue == null)
				return default;
			return FromValue<TValue>(obj, returnValue);
		}

		public static TValue CallStaticMethod<TValue>(IMemorySource source, [CallerMemberName] string name = "", object[] arguments = null)
		{
			Value returnValue = CallMethodCore(source.ParentContext, null, name, arguments);

			if (returnValue == null)
				return default;

			return returnValue.ValueCase switch
			{
				ValueOneofCase.Bit => (TValue)(object)returnValue.Bit,
				ValueOneofCase.Double => (TValue)(object)returnValue.Double,
				ValueOneofCase.Float => (TValue)(object)returnValue.Float,
				ValueOneofCase.Int32 => (TValue)(object)returnValue.Int32,
				ValueOneofCase.Int64 => (TValue)(object)returnValue.Int64,
				ValueOneofCase.Obj => HydrateObject<TValue>(source.ParentContext, returnValue.Obj),
				ValueOneofCase.Str => (TValue)(object)returnValue.Str,
				ValueOneofCase.Uint32 => (TValue)(object)returnValue.Uint32,
				ValueOneofCase.Uint64 => (TValue)(object)returnValue.Uint64,
				_ => default,
			};
		}

		public static void CallStaticMethod(IMemorySource source, [CallerMemberName] string name = "", object[] arguments = null)
		{
			CallMethodCore(source.ParentContext, null, name, arguments);
		}

		private static TValue HydrateObject<TValue>(IMemorySource source, Il2CppObject obj)
		{
			string typeName = Il2CppTypeName.GetTypeName(obj.Klass);
			Type objType = Types.LoadedTypes.GetType(typeName);
			if (objType == null)
				return default;

			// make sure we always fetch metadata for types at time of hydration
			// this is the one time we can be 100% sure of the concrete type that
			// obj->klass points to, and we don't get another chance to resolve
			// this type deterministically (nested types can't be looked up by name)
			if (!Il2CppTypeCache.HasType(source.ParentContext, objType))
			{
				ClassDefinition clsDef = source.ParentContext.ReadValue<ClassDefinition>(obj.Address);
				Il2CppTypeCache.GetTypeInfo(source.ParentContext, objType, clsDef.Address);
			}
			return (TValue)Activator.CreateInstance(objType, source, obj.Address);
		}

		public static TValue GetValue<TValue>(IRuntimeObject obj, string name, byte indirection = 1)
		{
			Il2CppTypeInfo typeInfo = Il2CppTypeCache.GetTypeInfo(obj.Source.ParentContext, typeof(TClass));
			Il2CppField fld = typeInfo.Fields.First(fld => fld.Name == name);
			Il2CppTypeCache.GetTypeInfo(obj.Source.ParentContext, typeof(TValue), fld.KlassAddr);
			return obj.Source.ReadValue<TValue>(obj.Address + fld.Offset, indirection);
		}

		public static TValue GetStaticValue<TValue>(Il2CsRuntimeContext context, string name, byte indirection = 1)
		{
			Il2CppTypeInfo typeInfo = Il2CppTypeCache.GetTypeInfo(context, typeof(TClass));
			Il2CppField fld = typeInfo.Fields.First(fld => fld.Name == name);
			Il2CppTypeCache.GetTypeInfo(context, typeof(TValue), fld.KlassAddr);
			return context.ReadValue<TValue>(
				typeInfo.StaticFieldsAddress + fld.Offset,
				indirection);
		}
	}

}
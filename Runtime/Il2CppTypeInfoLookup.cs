using Il2CppToolkit.Injection.Client;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using static Il2CppToolkit.Injection.Client.Value;

namespace Il2CppToolkit.Runtime
{
	public static class Il2CppTypeName
	{
		public static ClassId GetKlass(Type type)
		{
			return new() { Name = Il2CppTypeName.GetTypeName(type, false), Namespaze = type.Namespace };
		}
		public static string GetTypeName(Type type, bool includeFirst = true)
		{
			string typeName = includeFirst ? type.Namespace : "";
			if (!string.IsNullOrEmpty(typeName))
				typeName += ".";
			typeName += type.Name;

			if (type.IsConstructedGenericType)
			{
				typeName = typeName.Substring(0, typeName.Length - 2);
				typeName += "<";
				typeName += string.Join(",", type.GenericTypeArguments.Select(arg => GetTypeName(arg)));
				typeName += ">";
			}
			return typeName;
		}
	}
	public static class Il2CppTypeName<TClass>
	{
		public static ClassId klass = Il2CppTypeName.GetKlass(typeof(TClass));
	}

	public class Il2CppTypeInfoLookup<TClass>
	{
		private static GetTypeInfoResponse s_typeInfo;
		private static ClassId klass => Il2CppTypeName<TClass>.klass;

		public static Il2CppTypeInfo GetTypeInfo(InjectionClient client)
		{
			s_typeInfo ??= client.Il2Cpp.GetTypeInfo(CreateRequest(), null, DateTime.MaxValue, default);
			return s_typeInfo.TypeInfo;
		}

		private static GetTypeInfoRequest CreateRequest()
		{
			return new() { Klass = klass };
		}

		public static Value ValueFrom(object value)
		{
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
				IRuntimeObject obj => new Value() { Obj = new Il2CppObject() { Address = obj.Address, Klass = Il2CppTypeName.GetKlass(obj.GetType()) } },
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

		public static TValue CallMethod<TValue>(IRuntimeObject obj, string name, object[] arguments)
		{
			CallMethodRequest req = new()
			{
				Klass = klass,
				MethodName = name,
				Instance = obj == null ? null : new Il2CppObject() { Address = obj.Address, Klass = Il2CppTypeName<TClass>.klass },
			};
			req.Arguments.AddRange(arguments.Select(ValueFrom));
			CallMethodResponse response = obj.Source.ParentContext.InjectionClient.Il2Cpp.CallMethod(req);
			return response.ReturnValue.ValueCase switch
			{
				ValueOneofCase.Bit => (TValue)(object)response.ReturnValue.Bit,
				ValueOneofCase.Double => (TValue)(object)response.ReturnValue.Double,
				ValueOneofCase.Float => (TValue)(object)response.ReturnValue.Float,
				ValueOneofCase.Int32 => (TValue)(object)response.ReturnValue.Int32,
				ValueOneofCase.Int64 => (TValue)(object)response.ReturnValue.Int64,
				ValueOneofCase.Obj => (TValue)HydrateObject(obj.Source.ParentContext, response.ReturnValue.Obj),
				ValueOneofCase.Str => (TValue)(object)response.ReturnValue.Str,
				ValueOneofCase.Uint32 => (TValue)(object)response.ReturnValue.Uint32,
				ValueOneofCase.Uint64 => (TValue)(object)response.ReturnValue.Uint64,
				_ => default,
			};
		}

		private static object HydrateObject(IMemorySource source, Il2CppObject obj)
		{
			string className = obj.Klass.Namespaze;
			if (className.Length > 0)
				className += ".";
			className += obj.Klass.Name;
			Type objectType = Type.GetType(className, false, false);
			if (objectType == null)
				throw new EntryPointNotFoundException($"Class '{className}' could not be found");
			object value = Activator.CreateInstance(objectType, source, obj.Address);
			// TODO: store and handle 'handle' correctly
			return value;
		}

		public static TValue GetValue<TValue>(IRuntimeObject obj, string name, byte indirection = 1)
		{
			Il2CppTypeInfo typeInfo = GetTypeInfo(obj.Source.ParentContext.InjectionClient);
			Il2CppField fld = typeInfo.Fields.First(fld => fld.Name == name);
			return obj.Source.ReadValue<TValue>(obj.Address + fld.Offset, indirection);
		}

		public static TValue GetStaticValue<TValue>(Il2CsRuntimeContext context, string name, byte indirection = 1)
		{
			Il2CppTypeInfo typeInfo = GetTypeInfo(context.InjectionClient);
			Il2CppField fld = typeInfo.Fields.First(fld => fld.Name == name);
			return context.ReadValue<TValue>(typeInfo.StaticFieldsAddress + fld.Offset, indirection);
		}
	}

	public class NullableArg
	{
		internal bool HasValue = false;
		internal object Value = null;
	}
	public class NullableArg<T> : NullableArg where T : struct
	{
		internal T TypedValue;
		public NullableArg()
		{
			HasValue = false;
			Value = null;
			TypedValue = default;
		}
		public NullableArg(T? value)
		{
			HasValue = value.HasValue;
			Value = HasValue ? value.Value : null;
			TypedValue = HasValue ? value.Value : default;
		}
	}
}
using Il2CppToolkit.Common.Errors;
using Il2CppToolkit.Injection.Client;
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using static Il2CppToolkit.Injection.Client.Value;

namespace Il2CppToolkit.Runtime
{
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

		public static Value CallMethodCore(IRuntimeObject obj, [CallerMemberName] string name = "", object[] arguments = null)
		{

			if (arguments == null)
				throw new ArgumentNullException(nameof(arguments));

			CallMethodRequest req = new()
			{
				Klass = klass,
				MethodName = name,
				Instance = obj == null ? null : new Il2CppObject() { Address = obj.Address, Klass = Il2CppTypeName<TClass>.klass },
			};
			req.Arguments.AddRange(arguments.Select(ValueFrom));
			CallMethodResponse response = obj.Source.ParentContext.InjectionClient.Il2Cpp.CallMethod(req);
			return response.ReturnValue;
		}

		public static void CallMethod(IRuntimeObject obj, [CallerMemberName] string name = "", object[] arguments = null)
		{
			CallMethodCore(obj, name, arguments);
		}

		public static TValue CallMethod<TValue>(IRuntimeObject obj, [CallerMemberName] string name = "", object[] arguments = null)
		{
			Value returnValue = CallMethodCore(obj, name, arguments);

			if (returnValue == null)
				return default;

			return returnValue.ValueCase switch
			{
				ValueOneofCase.Bit => (TValue)(object)returnValue.Bit,
				ValueOneofCase.Double => (TValue)(object)returnValue.Double,
				ValueOneofCase.Float => (TValue)(object)returnValue.Float,
				ValueOneofCase.Int32 => (TValue)(object)returnValue.Int32,
				ValueOneofCase.Int64 => (TValue)(object)returnValue.Int64,
				ValueOneofCase.Obj => throw new InvalidOperationException(),
				ValueOneofCase.Str => (TValue)(object)returnValue.Str,
				ValueOneofCase.Uint32 => (TValue)(object)returnValue.Uint32,
				ValueOneofCase.Uint64 => (TValue)(object)returnValue.Uint64,
				_ => default,
			};
		}

		public static PinnedReference<TValue> CallMethodWithPinnedResult<TValue>(IRuntimeObject obj, [CallerMemberName] string name = "", object[] arguments = null)
			where TValue : class, IRuntimeObject
		{
			Value returnValue = CallMethodCore(obj, name, arguments);

			if (returnValue == null)
				return default;

			ErrorHandler.VerifyElseThrow(returnValue.ValueCase == ValueOneofCase.Obj, RuntimeError.InvalidValueCase, "Invalid value case");
			TValue objValue = HydrateObject<TValue>(obj.Source.ParentContext, returnValue.Obj);
			return new(objValue, returnValue.Obj.Handle);
		}

		private static TValue HydrateObject<TValue>(IMemorySource source, Il2CppObject obj)
		{
			return (TValue)Activator.CreateInstance(typeof(TValue), source, obj.Address);
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

}
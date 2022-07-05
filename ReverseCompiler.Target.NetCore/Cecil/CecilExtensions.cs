using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Mono.Cecil
{
	public static class CecilExtensions
	{
		public static void AddRange<T>(this Mono.Collections.Generic.Collection<T> self, params T[] items) => AddRange(self, (IEnumerable<T>)items);
		public static void AddRange<T>(this Mono.Collections.Generic.Collection<T> self, IEnumerable<T> items)
		{
			foreach (T item in items)
				self.Add(item);
		}
		public static string GetSafeName(this TypeReference self)
		{
			if (self.HasGenericParameters)
				return self.Name.Split('`')[0];
			return self.Name;
		}

		public static string GetFullSafeName(this TypeReference self)
		{
			return Regex.Replace(
				Regex.Replace(
					self.FullName, @"[<(\[].*[\])>]", ""),
				@"`\d*", "")
				.Replace('.', '_');
		}

		public static GenericInstanceType MakeGenericType(this TypeReference self, params TypeReference[] arguments) => MakeGenericType(self, (IEnumerable<TypeReference>)arguments);
		public static GenericInstanceType MakeGenericType(this TypeReference self, IEnumerable<TypeReference> arguments)
		{
			if (self.GenericParameters.Count != arguments.Count())
				throw new ArgumentOutOfRangeException(nameof(self));

			GenericInstanceType instance = new(self);

			foreach (var argument in arguments)
				instance.GenericArguments.Add(argument);

			return instance;
		}

		public static MethodReference GetConstructor(this TypeReference typeReference, params TypeReference[] arguments)
		{
			MethodReference methodRef = new(".ctor", typeReference.Module.TypeSystem.Void)
			{
				DeclaringType = typeReference,
				HasThis = true,
				ExplicitThis = false,
				CallingConvention = MethodCallingConvention.Default,
			};
			methodRef.Parameters.AddRange(arguments.Select(arg => new ParameterDefinition(arg)));
			return methodRef;
		}

		public static GenericInstanceMethod MakeGeneric(this MethodReference self, params TypeReference[] arguments)
		{
			GenericInstanceMethod reference = new(self);
			reference.GenericArguments.AddRange(arguments);
			return reference;
		}

		public static void EmitByte(this ILProcessor self, byte value)
		{
			switch (value)
			{
				case 0: self.Emit(OpCodes.Ldc_I4_0); break;
				case 1: self.Emit(OpCodes.Ldc_I4_1); break;
				case 2: self.Emit(OpCodes.Ldc_I4_2); break;
				case 3: self.Emit(OpCodes.Ldc_I4_3); break;
				case 4: self.Emit(OpCodes.Ldc_I4_4); break;
				case 5: self.Emit(OpCodes.Ldc_I4_5); break;
				case 6: self.Emit(OpCodes.Ldc_I4_6); break;
				case 7: self.Emit(OpCodes.Ldc_I4_7); break;
				case 8: self.Emit(OpCodes.Ldc_I4_8); break;
				default:
					throw new ArgumentOutOfRangeException(nameof(value));
			}
		}
		public static void EmitI4(this ILProcessor self, int value)
		{
			switch (value)
			{
				case 0: self.Emit(OpCodes.Ldc_I4_0); break;
				case 1: self.Emit(OpCodes.Ldc_I4_1); break;
				case 2: self.Emit(OpCodes.Ldc_I4_2); break;
				case 3: self.Emit(OpCodes.Ldc_I4_3); break;
				case 4: self.Emit(OpCodes.Ldc_I4_4); break;
				case 5: self.Emit(OpCodes.Ldc_I4_5); break;
				case 6: self.Emit(OpCodes.Ldc_I4_6); break;
				case 7: self.Emit(OpCodes.Ldc_I4_7); break;
				case 8: self.Emit(OpCodes.Ldc_I4_8); break;
				default:
					self.Emit(OpCodes.Ldc_I4_S, (sbyte)value); break;
			}
		}
		public static void EmitArg(this ILProcessor self, int value)
		{
			switch (value)
			{
				case 0: self.Emit(OpCodes.Ldarg_0); break;
				case 1: self.Emit(OpCodes.Ldarg_1); break;
				case 2: self.Emit(OpCodes.Ldarg_2); break;
				case 3: self.Emit(OpCodes.Ldarg_3); break;
				default:
					self.Emit(OpCodes.Ldarg_S, (byte)value); break;
			}
		}
	}
}
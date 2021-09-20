using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Il2CppToolkit.Model;
using static Il2CppToolkit.Model.Il2CppConstants;

namespace Il2CppToolkit.Common
{
	internal static class Helpers
	{
		public static void VerifyElseThrow(bool condition, string message)
		{
			if (condition)
			{
				return;
			}
			string errorMessage = $"Fatal error: {message}";
			Trace.WriteLine(errorMessage);
			if (Debugger.IsAttached)
			{
				Debugger.Break();
			}
			throw new ApplicationException(errorMessage);
		}
		public static void Assert(bool condition, string message)
		{
			if (condition)
			{
				return;
			}
			Trace.WriteLine($"Assertion failed: {message}");
			if (Debugger.IsAttached)
			{
				Debugger.Break();
			}
		}

		public static readonly Dictionary<int, Type> TypeMap = new Dictionary<int, Type>
		{
			{1,typeof(void)},
			{2,typeof(bool)},
			{3,typeof(char)},
			{4,typeof(sbyte)},
			{5,typeof(byte)},
			{6,typeof(short)},
			{7,typeof(ushort)},
			{8,typeof(int)},
			{9,typeof(uint)},
			{10,typeof(long)},
			{11,typeof(ulong)},
			{12,typeof(float)},
			{13,typeof(double)},
			{14,typeof(string)},
			{22,typeof(IntPtr)},
			{24,typeof(IntPtr)},
			{25,typeof(UIntPtr)},
			{28,typeof(object)},
		};

		public static TypeAttributes GetTypeAttributes(Il2CppTypeDefinition typeDef)
		{
			//return (TypeAttributes)typeDef.flags;
			TypeAttributes attrs = default;
			uint visibility = typeDef.flags & TYPE_ATTRIBUTE_VISIBILITY_MASK;
			switch (visibility)
			{
				case TYPE_ATTRIBUTE_PUBLIC:
					attrs |= TypeAttributes.Public;
					break;
				case TYPE_ATTRIBUTE_NESTED_PUBLIC:
					attrs |= TypeAttributes.NestedPublic;
					break;
				case TYPE_ATTRIBUTE_NOT_PUBLIC:
					attrs |= TypeAttributes.Public;
					// always public :)
					// attrs |= TypeAttributes.NotPublic;
					break;
				case TYPE_ATTRIBUTE_NESTED_FAM_AND_ASSEM:
					attrs |= TypeAttributes.NestedPublic;
					// always public :)
					// attrs |= TypeAttributes.NestedFamANDAssem;
					break;
				case TYPE_ATTRIBUTE_NESTED_ASSEMBLY:
					attrs |= TypeAttributes.NestedPublic;
					// always public :)
					// attrs |= TypeAttributes.NestedAssembly;
					break;
				case TYPE_ATTRIBUTE_NESTED_PRIVATE:
					attrs |= TypeAttributes.NestedPublic;
					// always public :)
					// attrs |= TypeAttributes.NestedPrivate;
					break;
				case TYPE_ATTRIBUTE_NESTED_FAMILY:
					attrs |= TypeAttributes.NestedPublic;
					// always public :)
					// attrs |= TypeAttributes.NestedFamily;
					break;
				case TYPE_ATTRIBUTE_NESTED_FAM_OR_ASSEM:
					attrs |= TypeAttributes.NestedPublic;
					// always public :)
					// attrs |= TypeAttributes.NestedFamORAssem;
					break;
			}
			if ((typeDef.flags & TYPE_ATTRIBUTE_ABSTRACT) != 0 && (typeDef.flags & TYPE_ATTRIBUTE_SEALED) != 0)
			{ } //	attrs |= TypeAttributes.Abstract | TypeAttributes.Sealed;
			else if ((typeDef.flags & TYPE_ATTRIBUTE_INTERFACE) == 0 && (typeDef.flags & TYPE_ATTRIBUTE_ABSTRACT) != 0)
				attrs |= TypeAttributes.Abstract;
			else if ((typeDef.flags & TYPE_ATTRIBUTE_SEALED) != 0)
				attrs |= TypeAttributes.Sealed;
			if ((typeDef.flags & TYPE_ATTRIBUTE_INTERFACE) != 0)
				attrs |= TypeAttributes.Interface | TypeAttributes.Abstract;
			if (((TypeAttributes)typeDef.flags).HasFlag(TypeAttributes.ExplicitLayout))
				attrs |= TypeAttributes.ExplicitLayout;
			if (((TypeAttributes)typeDef.flags).HasFlag(TypeAttributes.Serializable))
				attrs |= TypeAttributes.Serializable;
			if (((TypeAttributes)typeDef.flags).HasFlag(TypeAttributes.AnsiClass))
				attrs |= TypeAttributes.AnsiClass;
			return attrs;
		}

	}
}

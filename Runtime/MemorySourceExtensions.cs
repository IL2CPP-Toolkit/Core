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
        public static event EventHandler<MemoryAccessEventArgs> ObjectReadFromMemory;
        public static event EventHandler<MemoryAccessErrorEventArgs> ObjectReadError;

        private static readonly Dictionary<Type, Func<IMemorySource, ulong, object>> s_impl = new();
        static MemorySourceExtensions()
        {
            s_impl.Add(typeof(Char), (context, address) => BitConverter.ToChar(context.ReadMemory(address, sizeof(Char)).Span));
            s_impl.Add(typeof(Boolean), (context, address) => BitConverter.ToBoolean(context.ReadMemory(address, sizeof(Boolean)).Span));
            s_impl.Add(typeof(Double), (context, address) => BitConverter.ToDouble(context.ReadMemory(address, sizeof(Double)).Span));
            s_impl.Add(typeof(Single), (context, address) => BitConverter.ToSingle(context.ReadMemory(address, sizeof(Single)).Span));
            s_impl.Add(typeof(Int16), (context, address) => BitConverter.ToInt16(context.ReadMemory(address, sizeof(Int16)).Span));
            s_impl.Add(typeof(Int32), (context, address) => BitConverter.ToInt32(context.ReadMemory(address, sizeof(Int32)).Span));
            s_impl.Add(typeof(Int64), (context, address) => BitConverter.ToInt64(context.ReadMemory(address, sizeof(Int64)).Span));
            s_impl.Add(typeof(UInt16), (context, address) => BitConverter.ToUInt16(context.ReadMemory(address, sizeof(UInt16)).Span));
            s_impl.Add(typeof(UInt32), (context, address) => BitConverter.ToUInt32(context.ReadMemory(address, sizeof(UInt32)).Span));
            s_impl.Add(typeof(UInt64), (context, address) => BitConverter.ToUInt64(context.ReadMemory(address, sizeof(UInt64)).Span));
            s_impl.Add(typeof(IntPtr), (context, address) => (IntPtr)BitConverter.ToInt64(context.ReadMemory(address, sizeof(Int64)).Span));
            s_impl.Add(typeof(UIntPtr), (context, address) => (UIntPtr)BitConverter.ToInt64(context.ReadMemory(address, sizeof(UInt64)).Span));
        }


        public static T ReadValue<T>(this IMemorySource source, ulong address, byte indirection = 1)
        {
            return (T)ReadValue(source, typeof(T), address, indirection);
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
                if (type == typeof(string))
                {
                    return ReadString(source, address);
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
                return array.Array;
            }

            if (type.IsInterface || type.IsAbstract || type == typeof(Object))
            {
                UnknownClass unk = (UnknownClass)ReadStruct(source, typeof(UnknownClass), address);

                if (unk?.ClassDefinition == null)
                    return null;

                type = LoadedTypes.GetType(unk.ClassDefinition);

                if (type == null)
                    return null;

                if (type.IsGenericType)
                {
                    // TODO: Get generic type arguments at runtime
                    return null;
                }
            }
            if (type.IsAssignableTo(typeof(StructBase)))
            {
                object classObject = Activator.CreateInstance(type, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, null, new object[] { (IMemorySource)source, address }, null);
                return classObject;
            }
            // value type
            object valueObject = Activator.CreateInstance(type);
            ReadFields(source, type, valueObject, address);
            return valueObject;
        }

        public static void ReadFields(this IMemorySource source, Type type, object target, ulong targetAddress)
        {
            MethodInfo readFieldsOverride = type.GetMethod(
                "ReadFields",
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public,
                null,
                CallingConventions.HasThis,
                new[]
                {
                    typeof(IMemorySource),
                    typeof(ulong),
                },
                null);

            if (readFieldsOverride != null)
            {
                readFieldsOverride.Invoke(target, new object[] { (IMemorySource)source, targetAddress });
                return;
            }

            do
            {
                FieldInfo[] fields =
                    type.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                foreach (FieldInfo field in fields)
                {
                    try
                    {
                        if (field.Attributes.HasFlag(FieldAttributes.Literal))
                            continue;
                        ReadField(source, target, targetAddress, field);
                    }
                    catch (Exception)
                    { }
                }

                type = type.BaseType;
            } while (type != null && type != typeof(StructBase) && type != typeof(object) && type != typeof(ValueType));
        }

        private static void ReadField(this IMemorySource source, object target, ulong targetAddress, FieldInfo field)
        {
            if (field.GetCustomAttribute<IgnoreAttribute>(inherit: true) != null)
            {
                return;
            }
            ulong offset = targetAddress + source.ParentContext.GetMemberFieldOffset(field, targetAddress);
            byte indirection = 1;
            IndirectionAttribute indirectionAttr = field.GetCustomAttribute<IndirectionAttribute>(inherit: true);
            if (indirectionAttr != null)
            {
                indirection = indirectionAttr.Indirection;
            }

            object result = ReadValue(source, field.FieldType, offset, indirection);
            field.SetValue(target, result);
        }

        private static object ReadPrimitive(this IMemorySource context, Type type, ulong address)
        {
            if (s_impl.TryGetValue(type, out Func<IMemorySource, ulong, object> fn))
            {
                return fn(context, address);
            }
            throw new ArgumentException($"Type '{type.FullName}' is not a valid primitive type");
        }

        private static T ReadPrimitive<T>(this IMemorySource context, ulong address)
        {
            if (s_impl.TryGetValue(typeof(T), out Func<IMemorySource, ulong, object> fn))
            {
                return (T)fn(context, address);
            }
            throw new ArgumentException($"Type '{typeof(T).FullName}' is not a valid primitive type");
        }

        private static object GetDefaultValue(Type type)
        {
            if (type.IsAssignableTo(typeof(IEnumerable<>)))
            { }

            return null;
        }

        private static object ReadString(IMemorySource source, ulong address)
        {
            Native__String? str = source.ReadValue<Native__String>(address);
            return str?.Value;
        }

    }
}

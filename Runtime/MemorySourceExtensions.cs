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
        public static event EventHandler<MemoryAccessErrorEventArgs> ObjectWriteError;

        private class ConvertPrimitive
        {
            public Func<IMemorySource, ulong, object> ReadFn;
            public Action<IMemorySource, ulong, object> WriteFn;
        }

        private static readonly Dictionary<Type, ConvertPrimitive> s_implMap = new();
        static MemorySourceExtensions()
        {
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

        public static T WriteValue<T>(this IMemorySource source, ulong address, byte indirection, object value)
        {
            return (T)WriteValue(source, typeof(T), address, value, indirection);
        }

        public static object WriteValue(this IMemorySource source, Type type, ulong address, object value, byte indirection)
        {
            try
            {
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
                    factory.WriteValue(source, address, value);
                    return value;
                }
                // TODO: requires allocation >:O
                // if (type == typeof(string))
                // {
                //     return WriteString(source, address);
                // }
                if (type.IsEnum)
                {
                    WritePrimitive(source, type.GetEnumUnderlyingType(), address, value);
                    return value;
                }
                if (type.IsPrimitive)
                {
                    WritePrimitive(source, type, address, value);
                    return value;
                }
                throw new InvalidOperationException($"Type '{type.FullName}' does not support writing");
            }
            catch (Exception ex)
            {
                ObjectWriteError?.Invoke(source, new(type, address, ex));
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

            Type originalType = type;
            if (type.IsInterface || type.IsAbstract || type == typeof(Object))
            {
                UnknownClass unk = (UnknownClass)ReadStruct(source, typeof(UnknownClass), address);

                if (unk?.ClassDefinition == null)
                    return originalType == typeof(object) ? unk : null;

                type = LoadedTypes.GetType(unk.ClassDefinition);

                if (type == null)
                    return originalType == typeof(object) ? unk : null;

                if (type.IsGenericType)
                {
                    // TODO: Get generic type arguments at runtime
                    return originalType == typeof(object) ? unk : null;
                }
            }
            if (type.IsAssignableTo(typeof(StructBase)))
            {
                object classObject = Activator.CreateInstance(type, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, null, new object[] { (IMemorySource)source, address }, null);
                return classObject;
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

        private static void WriteField(this IMemorySource source, object target, ulong targetAddress, FieldInfo field, object value)
        {
            ulong offset = targetAddress + source.ParentContext.GetMemberFieldOffset(field, targetAddress);
            byte indirection = 1;
            IndirectionAttribute indirectionAttr = field.GetCustomAttribute<IndirectionAttribute>(inherit: true);
            if (indirectionAttr != null)
            {
                indirection = indirectionAttr.Indirection;
            }

            object result = WriteValue(source, field.FieldType, offset, value, indirection);
            field.SetValue(target, result);
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

        private static void WritePrimitive(this IMemorySource context, Type type, ulong address, object value)
        {
            if (!s_implMap.TryGetValue(type, out var impl))
                throw new ArgumentException($"Type '{type.FullName}' is not a valid primitive type");
            impl.WriteFn(context, address, value);
        }

        private static void WritePrimitive<T>(this IMemorySource context, ulong address, T value)
        {
            if (!s_implMap.TryGetValue(typeof(T), out var impl))
                throw new ArgumentException($"Type '{typeof(T).FullName}' is not a valid primitive type");
            impl.WriteFn(context, address, value);
        }

        private static object GetDefaultValue(Type type)
        {
            if (type.IsAssignableTo(typeof(IEnumerable<>)))
            { }

            return null;
        }
    }
}

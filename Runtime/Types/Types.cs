﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace Il2CppToolkit.Runtime.Types
{
    public static class Types
    {
        public static bool TryGetType(uint token, out Type mappedType)
        {
            return TokenMapping.TryGetValue(token, out mappedType);
        }

        public static bool TryGetType(string typeName, out Type mappedType)
        {
            if (NativeMapping.TryGetValue(typeName, out mappedType))
            {
                return true;
            }
            if (typeName.StartsWith("System."))
            {
                mappedType = null;
                return true;
            }
            return false;
        }
        private static readonly Dictionary<string, Type> NativeMapping = new();
        private static readonly Dictionary<uint, Type> TokenMapping = new();
        static Types()
        {
            NativeMapping.Add(typeof(ValueType).FullName, typeof(ValueType));
            foreach (var (mapFrom, mapTo) in GetTypesWithMappingAttribute(typeof(Types).Assembly))
            {
                NativeMapping.Add(mapFrom.FullName, mapTo);
            }
            foreach (var (mapFrom, mapTo) in GetTypesWithTokenAttribute(typeof(Types).Assembly))
            {
                TokenMapping.Add(mapFrom, mapTo);
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
        static IEnumerable<(uint, Type)> GetTypesWithTokenAttribute(Assembly assembly)
        {
            foreach (Type type in assembly.GetTypes())
            {
                TokenAttribute ta = type.GetCustomAttribute<TokenAttribute>(true);
                if (ta != null)
                {
                    yield return (ta.Token, type);
                }
            }
        }

        public static readonly Dictionary<Type, int> TypeSizes = new()
        {
            { typeof(void), 0 },
            { typeof(bool), sizeof(bool) },
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
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Il2CppToolkit.Common;

namespace Il2CppToolkit.Model
{
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class TypeDescriptor
    {
        public TypeDescriptor(string name, Il2CppTypeDefinition typeDef, int typeIndex, Il2CppImageDefinition imageDef)
        {
            m_name = name;
            TypeDef = typeDef;
            TypeIndex = typeIndex;
            ImageDef = imageDef;
        }

        public string Tag
        {
            get
            {
                return Utilities.GetTypeTag(TypeDef.nameIndex, TypeDef.namespaceIndex, TypeDef.token);
            }
        }

        public string Name
        {
            get
            {
                if (GenericParameterNames.Length == 0)
                {
                    return m_name;
                }
                return $"{m_name}`{GenericParameterNames.Length}";
            }
        }
        public string FullName
        {
            get
            {
                if (DeclaringParent != null)
                {
                    return $"{DeclaringParent.FullName}+{m_name.Split('.').Last()}";
                }
                return Name;
            }
        }

        private readonly string m_name;

        public bool IsStatic
        {
            get
            {
                return ((TypeAttributes)TypeDef.flags).HasFlag(TypeAttributes.Abstract) && ((TypeAttributes)TypeDef.flags).HasFlag(TypeAttributes.Sealed);
            }
        }
        public readonly Il2CppImageDefinition ImageDef;
        public readonly Il2CppTypeDefinition TypeDef;
        public readonly int TypeIndex;
        public readonly List<ITypeReference> Implements = new();
        public readonly List<TypeDescriptor> NestedTypes = new();
        public TypeDescriptor DeclaringParent;
        public TypeDescriptor GenericParent;
        public ITypeReference[] GenericTypeParams;
        public ITypeReference Base;
        public TypeAttributes Attributes;
        public uint SizeInBytes;
        public string[] GenericParameterNames = Array.Empty<string>();
        public readonly List<FieldDescriptor> Fields = new();
        public readonly List<PropertyDescriptor> Properties = new();
        public readonly List<MethodDescriptor> Methods = new();

        public TypeInfoAddress TypeInfo;

        private string DebuggerDisplay => string.Join(" : ", Name, Base?.Name).TrimEnd(new char[] { ' ', ':' });
    }
    public class TypeInfoAddress
    {
        public ulong Address;
        public string ModuleName;
    }
}

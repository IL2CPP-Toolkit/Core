using System;
using System.Collections.Generic;
using System.Reflection;
using Il2CppToolkit.Common;
using Il2CppToolkit.Common.Errors;
using Il2CppToolkit.Runtime.Types.Reflection;

namespace Il2CppToolkit.Runtime.Types
{
    public static class LoadedTypes
    {
        private static readonly Dictionary<string, Type> s_tokenToType = new();

        public static Type GetType(ClassDefinition classDef)
        {
            var classType = classDef.byval_arg.TypeHandle;
            string token = Utilities.GetTypeTag(classType._nameIndex, classType._namespaceIndex, classDef.Token);
            return s_tokenToType.TryGetValue(token, out Type retVal) ? retVal : null;
        }

        static LoadedTypes()
        {
            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (asm.GetCustomAttribute<GeneratedAttribute>() == null) continue;

                foreach (Type type in asm.GetTypes())
                {
                    TagAttribute attr = type.GetCustomAttribute<TagAttribute>();
                    if (attr == null) continue;

                    ErrorHandler.Assert(s_tokenToType.TryAdd(attr.Tag, type), "Duplicate type by token");
                }
            }
        }
    }
}

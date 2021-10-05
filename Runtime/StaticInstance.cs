﻿using System;
using System.Reflection;
using Il2CppToolkit.Runtime.Types.Reflection;
using Il2CppToolkit.Common.Errors;

namespace Il2CppToolkit.Runtime
{
    public class StaticInstance<T> : StructBase
    {
        public override ClassDefinition ClassDefinition
        {
            get
            {
                return default;
            }
        }

        protected StaticInstance(Il2CsRuntimeContext context, ulong address) : base(context, address)
        {
        }

        // ReSharper disable once UnusedMember.Global
        public static T GetInstance(Il2CsRuntimeContext context)
        {
            AddressAttribute attr = typeof(T).GetCustomAttribute<AddressAttribute>();
            ErrorHandler.VerifyElseThrow(attr != null, RuntimeError.StaticAddressMissing, "Class does not have a known address defined in metadata");
            ulong address = attr.Address + context.GetModuleAddress(attr.RelativeToModule);
            ClassDefinition classDef = context.ReadValue<ClassDefinition>(address);
            return classDef.StaticFields.As<T>();
        }
    }
}

using System;
using System.Reflection;
using IL2CS.Core;
using IL2CS.Runtime.Types.Reflection;

namespace IL2CS.Runtime
{
	public class StaticInstance<T> : StructBase
	{
		public override ClassDefinition ClassDefinition
		{
			get
			{
				return Context.ReadValue<ClassDefinition>(Address);
			}
		}

		protected StaticInstance(Il2CsRuntimeContext context, ulong address) : base(context, address)
		{
		}

		// ReSharper disable once UnusedMember.Global
		public static T GetInstance(Il2CsRuntimeContext context)
		{
			AddressAttribute attr = typeof(T).GetCustomAttribute<AddressAttribute>();
			if (attr == null)
			{
				throw new ApplicationException("Class does not have a known address defined in metadata");
			}
			ulong address = attr.Address + context.GetModuleAddress(attr.RelativeToModule);
			return context.ReadValue<T>(address);
		}
	}
}

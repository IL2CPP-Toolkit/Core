using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IL2CS.Runtime.Types.Reflection
{
	public class StaticPropertyDefinition<T> : MethodDefinition
	{
		private readonly ulong m_fieldOffset;
		private readonly byte m_indirection;

		public StaticPropertyDefinition(ulong address, ulong fieldOffset, byte indirection, string moduleName)
			: base(address, moduleName)
		{
			m_fieldOffset = fieldOffset;
			m_indirection = indirection;
		}

		public T Get(Il2CsRuntimeContext context)
		{
			return context.ReadValue<T>(GetMethodInfo(context).DeclaringClass.StaticFields.Address + m_fieldOffset, m_indirection);
		}
	}
}

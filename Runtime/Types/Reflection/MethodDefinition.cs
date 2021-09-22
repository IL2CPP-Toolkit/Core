using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace IL2CS.Runtime.Types.Reflection
{
	public class MethodDefinition
	{
		private readonly ulong m_address;
		private readonly string m_moduleName;
		public MethodDefinition(ulong address, string moduleName)
		{
			m_address = address;
			m_moduleName = moduleName;
		}
		
		public NativeMethodInfo GetMethodInfo(Il2CsRuntimeContext context)
		{
			ulong address = m_address + context.GetModuleAddress(m_moduleName);
			return context.ReadValue<NativeMethodInfo>(address, 1);
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Il2CppToolkit.Runtime.Types;
using Il2CppToolkit.Runtime.Types.Reflection;

namespace Il2CppToolkit.Runtime
{
	public class UnknownClass : RuntimeObject
	{
		public UnknownClass(IMemorySource source, ulong address) : base(source, address)
		{
		}

		private ClassDefinition m_classDef;
		public virtual ClassDefinition ClassDefinition
		{
			get
			{
				if (m_classDef == null)
					m_classDef = Source.ReadValue<ClassDefinition>(Address);
				return m_classDef;
			}
		}
	}
}

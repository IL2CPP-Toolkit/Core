using System.Diagnostics;
using Il2CppToolkit.Runtime.Types.corelib;

namespace Il2CppToolkit.Runtime.Types.Reflection
{
	// [Size(4376)]
	// [Size(0x128)]
	[DebuggerDisplay("{DebuggerDisplay,nq}")]
	public class ClassDefinition : RuntimeObject
	{
		public ClassDefinition(IMemorySource source, ulong address) : base(source, address)
		{
		}

		private string DebuggerDisplay
		{
			get
			{
				if (string.IsNullOrEmpty(Namespace) && string.IsNullOrEmpty(Name))
				{
					return "(none)";
				}
				return FullName;
			}
		}

		public string FullName
		{
			get
			{
				if (Parent == null)
				{
					return GetLocalTypeName(Namespace, Name);
				}
				else
				{
					return $"{Parent.FullName}+{Name}";
				}
			}
		}

		private static string GetLocalTypeName(string ns, string name)
		{
			if (string.IsNullOrEmpty(ns))
			{
				return name;
			}
			else
			{
				return $"{ns}.{name}";
			}
		}

		private string m_name = null;
		public string Name
		{
			get
			{
				if (m_name == null)
					m_name = Source.ReadValue<Native__LPSTR>(Address + 0x10, 1).Value;
				return m_name;
			}
		}

		private string m_namespace = null;
		public string Namespace
		{
			get
			{
				if (m_namespace == null)
					m_namespace = Source.ReadValue<Native__LPSTR>(Address + 0x18, 1).Value;
				return m_namespace;
			}
		}

		private ClassDefinition m_parent;
		public ClassDefinition Parent
		{
			get
			{
				if (m_parent == null)
					m_parent = Source.ReadValue<ClassDefinition>(Address + 0x50, 1);
				return m_parent;
			}
		}

		private ClassDefinition m_base;
		public ClassDefinition Base
		{
			get
			{
				if (m_base == null)
					m_base = Source.ReadValue<ClassDefinition>(Address + 0x58, 1);
				return m_base;
			}
		}
	}
}

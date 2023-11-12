using Il2CppToolkit.Model;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Il2CppToolkit.Target.TSDef;

public class ReplacementTypes
{
	static ReplacementTypes()
	{
		Record = new("Record");
	}

	public static TSExternalReference Record { get; }

	public bool TryReplaceType(TypeDescriptor td, [NotNullWhen(true)] out TSTypeReference? typeRef)
	{
		if (td.FullName == "System.Collections.Generic.Dictionary`1")
		{
			typeRef = Record;
			return true;
		}
		typeRef = null;
		return false;
	}
}

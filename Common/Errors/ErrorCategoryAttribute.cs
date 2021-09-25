using System;
using System.Linq;

namespace Il2CppToolkit.Common.Errors
{
	[AttributeUsage(AttributeTargets.Enum)]
	public class ErrorCategoryAttribute : Attribute
	{
		public string Name { get; }
		public string Abbreviation { get; }
		public ErrorCategoryAttribute(string name)
		{
			Name = name;
			Abbreviation = string.Concat(name.Where(c => char.IsUpper(c)));
		}

		public ErrorCategoryAttribute(string name, string abbreviation)
		{
			Name = name;
			Abbreviation = abbreviation;
		}
	}
}
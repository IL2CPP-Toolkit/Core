﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Il2CppToolkit.Model
{
	[AttributeUsage(AttributeTargets.Field)]
	class ArrayLengthAttribute : Attribute
	{
		public int Length { get; set; }
	}
}

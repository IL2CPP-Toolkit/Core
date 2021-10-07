using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Il2CppToolkit.Runtime.Types;

namespace Il2CppToolkit.Runtime
{
    public class UnknownClass : StructBase
    {
        public UnknownClass(IMemorySource source, ulong address) : base(source, address)
        {
        }
    }
}

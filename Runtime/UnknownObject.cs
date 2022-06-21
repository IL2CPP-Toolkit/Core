using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Il2CppToolkit.Runtime.Types;

namespace Il2CppToolkit.Runtime
{
    public class UnknownObject : RuntimeObject
    {
        public UnknownObject() : base() { }
        public UnknownObject(IMemorySource source, ulong address) : base(source, address) { }
    }
}

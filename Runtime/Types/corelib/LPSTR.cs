using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Il2CppToolkit.Runtime.Types.corelib
{
    public struct Native__LPSTR
    {
        public string Value;
        private void ReadFields(IMemorySource source, ulong address)
        {
            address = source.ReadPointer(address);
            ReadOnlyMemory<byte> stringData = source.ReadMemory(address, 512);
            Value = Encoding.UTF8.GetString(stringData.Span).Split('\0', 2)[0];
        }
    }
}

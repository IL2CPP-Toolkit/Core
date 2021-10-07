using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Il2CppToolkit.Common.Errors;

namespace Il2CppToolkit.Runtime.Types.corelib
{
    public struct Native__String
    {
        public string Value;
        private void ReadFields(IMemorySource source, ulong address)
        {
            int strlen = source.ReadValue<int>(address + 16);
            if (strlen <= 0)
            {
                Value = string.Empty;
                ErrorHandler.Assert(strlen == 0, "Invalid string length");
                return;
            }

            ReadOnlyMemory<byte> stringData = source.ReadMemory(address + 20, (ulong)strlen * 2);
            Value = Encoding.Unicode.GetString(stringData.Span);
        }
    }
}

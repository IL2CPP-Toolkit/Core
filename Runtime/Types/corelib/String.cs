using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Il2CppToolkit.Common.Errors;

namespace Il2CppToolkit.Runtime.Types.corelib
{
    public struct Native__String : IRuntimeObject
    {
        public Native__String() => (Source, Address) = (default, default);
        public Native__String(IMemorySource source, ulong address) => (Source, Address) = (source, address);
        public IMemorySource Source { get; }
        public ulong Address { get; }

        // private fields
        private int m_stringLength => Il2CppTypeInfoLookup<string>.GetValue<int>(this, nameof(m_stringLength));
        private string cachedValue = null;

        public string Value
		{
			get
			{
                if (cachedValue != null)
                    return cachedValue;

                if (m_stringLength <= 0)
                {
                    cachedValue = string.Empty;
                    ErrorHandler.Assert(m_stringLength == 0, "Invalid string length");
                    return cachedValue;
                }

                var typeInfo = Il2CppTypeInfoLookup<string>.GetTypeInfo(Source.ParentContext.InjectionClient);
                ReadOnlyMemory<byte> stringData = Source.ReadMemory(
                    Address + typeInfo.Fields.First(fld => fld.Name == "m_firstChar").Offset, 
                    (ulong)m_stringLength * 2);
#if NET472
                cachedValue = Encoding.Unicode.GetString(stringData.Span.ToArray());
#else
                cachedValue = Encoding.Unicode.GetString(stringData.Span);
#endif
                return cachedValue;
            }
        }
    }
}

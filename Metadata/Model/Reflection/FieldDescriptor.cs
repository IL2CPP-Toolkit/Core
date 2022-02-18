using System.Reflection;
using System.Text.RegularExpressions;

namespace Il2CppToolkit.Model
{
    public class FieldDescriptor
    {
        private static readonly Regex BackingFieldRegex = new("<(.+)>k__BackingField", RegexOptions.Compiled);

        public FieldDescriptor(string name, ITypeReference typeReference, FieldAttributes attrs, ulong offset)
        {
            Name = BackingFieldRegex.Replace(name, match => match.Groups[1].Value);
            // this is kinda evil, but it will make them consistent in name at least =)
            StorageName = $"<{Name}>k__BackingField";
            Type = typeReference;
            Attributes = attrs;
            Offset = offset;
        }

        public readonly string StorageName;
        public readonly string Name;
        public readonly ITypeReference Type;
        public FieldAttributes Attributes;
        public readonly ulong Offset;
        public object DefaultValue;
    }
}

namespace Il2CppToolkit.Common
{
    public static class Utilities
    {
        public static ulong GetTypeTag(int discriminator, uint typeToken)
        {
            return ((ulong)discriminator << 32) + typeToken;
        }
        public static ulong GetTypeTag(uint discriminator, uint typeToken)
        {
            return ((ulong)discriminator << 32) + typeToken;
        }
        public static string GetTypeTag(long nameIndex, long namespaceIndex, long typeToken)
        {
            return $"{nameIndex}.{namespaceIndex}.{typeToken}";
        }
    }
}

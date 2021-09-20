namespace Il2CppToolkit.Common
{
	public static class Utilities
	{
		public static ulong GetTypeTag(int imageTypeStart, uint typeToken)
		{
			return ((ulong)imageTypeStart << 32) + typeToken;
		}
	}
}

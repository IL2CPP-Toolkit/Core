namespace Il2CppToolkit.Runtime
{
	public class NullableArg
	{
		internal bool HasValue = false;
		internal object Value = null;
	}

	public class NullableArg<T> : NullableArg where T : struct
	{
		internal T TypedValue;
		public NullableArg()
		{
			HasValue = false;
			Value = null;
			TypedValue = default;
		}
		public NullableArg(T? value)
		{
			HasValue = value.HasValue;
			Value = HasValue ? value.Value : null;
			TypedValue = HasValue ? value.Value : default;
		}
	}
}
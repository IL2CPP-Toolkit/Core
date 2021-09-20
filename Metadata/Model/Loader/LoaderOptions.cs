using System;

namespace Il2CppToolkit.Model
{
	public class LoaderOptions
	{
		public class ResolveFatPlatformEventArgs : EventArgs
		{
			public Fat[] Fats { get; }
			public int ResolveToIndex { get; set; } = -1;
			public ResolveFatPlatformEventArgs(Fat[] fats) => Fats = fats;
		}

		public event EventHandler<ResolveFatPlatformEventArgs> ResolveFatPlatform;

		public double? ForceVersion { get; set; }
		public ulong? GlobalMetadataDumpAddress { get; set; }

		internal void FireResolveFatPlatform(object sender, ResolveFatPlatformEventArgs args)
		{
			ResolveFatPlatform.Invoke(sender, args);
		}
	}
}
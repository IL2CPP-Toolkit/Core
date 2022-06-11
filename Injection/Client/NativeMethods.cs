using System.Runtime.InteropServices;

namespace Il2CppToolkit.Injection.Client
{
	internal static class NativeMethods
	{
		public struct PublicState
		{
			public int port;
		}

		[DllImport("Il2CppToolkit.Injection.Host.dll")]
		public static extern int InjectHook(uint dwProcId);
		[DllImport("Il2CppToolkit.Injection.Host.dll")]
		public static extern int ReleaseHook(uint dwProcId);
		[DllImport("Il2CppToolkit.Injection.Host.dll")]
		public static extern int GetState(uint dwProcId, ref PublicState state, int dwTimeoutMs = 3000);
	}
}

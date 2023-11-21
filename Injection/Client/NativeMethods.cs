using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Il2CppToolkit.Injection.Client
{
	internal static class NativeMethods
	{
		static NativeMethods()
		{
			try
			{
				// make a copy so the installed version does not have a lock
				string sourceDll = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Il2CppToolkit.Injection.Host.dll");
				string workingDll = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "$Il2CppToolkit.Injection.Host.dll");
				File.Copy(sourceDll, workingDll, true);
				File.SetAttributes(workingDll, FileAttributes.Hidden);
			}
			catch { }
		}
		public struct PublicState
		{
			public int port;
		}

		[DllImport("$Il2CppToolkit.Injection.Host.dll")]
		public static extern int InjectHook(uint dwProcId);
		[DllImport("$Il2CppToolkit.Injection.Host.dll")]
		public static extern int ReleaseHook(uint dwProcId);
		[DllImport("$Il2CppToolkit.Injection.Host.dll")]
		public static extern int GetState(uint dwProcId, ref PublicState state, int dwTimeoutMs = 3000);
	}
}

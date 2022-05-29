using Grpc.Net.Client;
using Raid.Toolkit.Interop;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Il2CppToolkit.Injection.Client
{
	internal class Program
	{
		static async Task Main()
		{
			Process proc = Process.GetProcessesByName("Notepad")[0];
			int result = NativeMethods.InjectHook((uint)proc.Id);
			NativeMethods.PublicState state = new();
			result = NativeMethods.GetState((uint)proc.Id, ref state);
			Thread.Sleep(5*60*1000);
		}
	}
}

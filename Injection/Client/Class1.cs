using Grpc.Core;
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
			Process proc = Process.Start(
				//@"C:\Program Files (x86)\SciTE\SciTE.exe"
				@"D:\git\github\IL2Cpp-Toolkit\Core\Injection\Sample\Sample.exe"
			);

			//Process proc = Process.GetProcessesByName("SciTE")[0];
			int result = NativeMethods.InjectHook((uint)proc.Id);
			if (result < 0)
			{
				Console.WriteLine($"Error: {result}");
				return;
			}
			NativeMethods.PublicState state = new();
			result = NativeMethods.GetState((uint)proc.Id, ref state, 30000);
			if (result != 0)
			{
				Console.WriteLine($"Error: {result}");
				return;
			}

			GrpcChannel channel = GrpcChannel.ForAddress($"http://localhost:{state.port}");
			var client = new MessageService.MessageServiceClient(channel);
			MessageReply reply = client.SendMessage(new MessageRequest { Msg = "foo" });
			Console.WriteLine(reply.Reply);

			proc.Kill();
		}
	}
}

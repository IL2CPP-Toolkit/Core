using Google.Protobuf.Collections;
using Grpc.Core;
using Grpc.Net.Client;
using Il2CppToolkit.Interop;
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
			//Process proc = Process.Start(
			//	//@"C:\Program Files (x86)\SciTE\SciTE.exe"
			//	@"D:\git\github\IL2Cpp-Toolkit\Core\Injection\Sample\Sample.exe"
			//);


			Process proc = Process.GetProcessesByName("Raid")[0];
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

			GrpcChannel channel = GrpcChannel.ForAddress($"http://localhost:{state.port}", new GrpcChannelOptions() { });
			var client = new Il2CppService.Il2CppServiceClient(channel);
			try
			{
				var response = client.FindClass(new() { Klass = new() { Name = "Client.App.SingleInstance<Client.Model.AppModel>" } }, deadline: DateTime.MaxValue);
				Console.WriteLine($"Class address @{response.Address:X16}");

				CallMethodRequest request = new()
				{
					Klass = new() { Name = "UnityEngine.Time" },
					MethodName = "set_timeScale",
					InstanceAddress = 0
				};
				request.Arguments.Add(new Value() { Float = 1.0f });
				CallMethodResponse rsp = client.CallMethod(request);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error: {ex.Message}");
			}
			//var client = new MessageService.MessageServiceClient(channel);
			//MessageReply reply = client.SendMessage(new MessageRequest { Msg = "foo" });
			//Console.WriteLine(reply.Reply);

			//proc.Kill();
		}
	}
}

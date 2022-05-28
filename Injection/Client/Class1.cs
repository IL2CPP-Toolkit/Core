using Grpc.Net.Client;
using Raid.Toolkit.Interop;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Il2CppToolkit.Injection.Client
{
	internal class Program
	{
		static async Task Main()
		{
			GrpcChannel channel = GrpcChannel.ForAddress("http://localhost:50051");
			MessageService.MessageServiceClient client = new (channel);

			Stopwatch sw = new();
			await client.SendMessageAsync(new() { Msg = "Ping" });
			await client.SendMessageAsync(new() { Msg = "Ping" });
			await client.SendMessageAsync(new() { Msg = "Ping" });
			sw.Start();
			var response = await client.SendMessageAsync(new() { Msg = "Ping" });
			sw.Stop();

			Console.WriteLine(response.Reply);
			Console.WriteLine(sw.Elapsed);
		}
	}
}

using Grpc.Net.Client;
using System;
using System.Diagnostics;

namespace Il2CppToolkit.Injection.Client
{
	public class InjectionClient : IDisposable
	{
		private readonly uint Pid;
		private bool IsDisposed;
		private GrpcChannel Channel;
		public Il2CppService.Il2CppServiceClient Il2Cpp { get; private set; }

		public InjectionClient(Process process)
		{
			Pid = (uint)process.Id;
			int result = NativeMethods.InjectHook(Pid);
			if (result != 0)
				throw new EntryPointNotFoundException("Could not inject process");
			NativeMethods.PublicState state = new();
			result = NativeMethods.GetState(Pid, ref state, 30000);
			if (result != 0)
				throw new EntryPointNotFoundException("Could not connect to process");
			Channel = GrpcChannel.ForAddress($"http://localhost:{state.port}", new GrpcChannelOptions() { });
			Il2Cpp = new Il2CppService.Il2CppServiceClient(Channel);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!IsDisposed)
			{
				if (disposing)
				{
					Channel.Dispose();
				}
				NativeMethods.ReleaseHook(Pid);
				Channel = null;
				Il2Cpp = null;
				IsDisposed = true;
			}
		}

		~InjectionClient()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: false);
		}

		public void Dispose()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
	}
}

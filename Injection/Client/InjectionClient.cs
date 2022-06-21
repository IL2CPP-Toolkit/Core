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
		private ProcessHook Hook;
		public Il2CppService.Il2CppServiceClient Il2Cpp { get; private set; }

		public InjectionClient(Process process)
		{
			Pid = (uint)process.Id;
			Hook = new(Pid);
			Channel = GrpcChannel.ForAddress($"http://localhost:{Hook.State.port}", new GrpcChannelOptions());
			Il2Cpp = new Il2CppService.Il2CppServiceClient(Channel);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!IsDisposed)
			{
				if (disposing)
				{
					Channel.Dispose();
					Hook.Dispose();
				}
				NativeMethods.ReleaseHook(Pid);
				Channel = null;
				Hook = null;
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

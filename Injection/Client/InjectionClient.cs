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
		public InjectionService.InjectionServiceClient Injection { get; private set; }

		public InjectionClient(Process process)
		{
			Pid = (uint)process.Id;
			Hook = new(Pid);
			Channel = GrpcChannel.ForAddress($"http://localhost:{Hook.State.port}", new GrpcChannelOptions());
			Il2Cpp = new(Channel);
			Injection = new(Channel);
			Injection.RegisterProcess(new RegisterProcessRequest { Pid = (uint)Environment.ProcessId });
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!IsDisposed)
			{
				if (disposing)
				{
					Injection.DeregisterProcess(new RegisterProcessRequest { Pid = (uint)Environment.ProcessId });
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

using Grpc.Net.Client;

using System;
using System.Diagnostics;

namespace Il2CppToolkit.Injection.Client
{
	public class InjectionClient : IDisposable
	{
		private readonly uint CurrentPid;
		private readonly uint TargetPid;
		private bool IsDisposed;
		private GrpcChannel Channel;
		private ProcessHook Hook;
		public Il2CppService.Il2CppServiceClient Il2Cpp { get; private set; }
		public InjectionService.InjectionServiceClient Injection { get; private set; }

		public InjectionClient(Process process)
		{
			TargetPid = (uint)process.Id;
#if NET6_0_OR_GREATER
			CurrentPid = (uint)Environment.ProcessId;
#else
			CurrentPid = (uint)Process.GetCurrentProcess().Id;
#endif
			Hook = new(TargetPid);
			Channel = GrpcChannel.ForAddress($"http://localhost:{Hook.State.port}", new GrpcChannelOptions()
			{
				MaxReceiveMessageSize = 1024 * 1024 * 256,
			});
			Il2Cpp = new(Channel);
			Injection = new(Channel);
			Injection.RegisterProcess(new RegisterProcessRequest { Pid = (uint)CurrentPid });
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!IsDisposed)
			{
				if (disposing)
				{
					try
					{
						Injection.DeregisterProcess(new RegisterProcessRequest { Pid = (uint)CurrentPid });
						Channel.Dispose();
					}
					catch { }
					Hook.Dispose();
				}
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

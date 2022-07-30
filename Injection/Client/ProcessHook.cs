using System;
using System.Threading;

namespace Il2CppToolkit.Injection.Client
{
	internal class ProcessHook : IDisposable
	{
		private readonly uint ProcessId;
		private readonly AsyncHookThreadDispatcher Dispatcher;
		private bool IsDisposed;

		private NativeMethods.PublicState _state = new();
		public NativeMethods.PublicState State => _state;

		public ProcessHook(uint processId, int timeout = 3000)
		{
			ProcessId = processId;

			// don't wait for the full timeout for initial state check
			// (we should only wait immediately after a call to InjectHook)
			int hrGetState = NativeMethods.GetState(ProcessId, ref _state, 10);
			if (hrGetState == 0)
				return;

			Dispatcher = new();

			Dispatcher.WaitFor(() =>
			{
				int result = NativeMethods.InjectHook(ProcessId);
				if (result != 0)
					throw new EntryPointNotFoundException("Could not inject process");
				result = NativeMethods.GetState(ProcessId, ref _state, timeout);
				if (result != 0)
					throw new EntryPointNotFoundException("Could not connect to process");
			});
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!IsDisposed)
			{
				if (disposing)
				{
					Dispatcher?.WaitFor(() => _ = NativeMethods.ReleaseHook(ProcessId));
					Dispatcher?.Dispose();
				}

				IsDisposed = true;
			}
		}

		~ProcessHook()
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

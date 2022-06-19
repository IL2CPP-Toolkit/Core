using System;
using System.Threading;

namespace Il2CppToolkit.Injection.Client
{
    internal class ProcessHook : IDisposable
    {
        private readonly uint ProcessId;
        private bool disposedValue;

        private NativeMethods.PublicState _state = new();
        public NativeMethods.PublicState State => _state;

        public ProcessHook(uint processId, int timeout = 30000)
        {
            ProcessId = processId;

            AsyncHookThread.Current.WaitFor(() => {
                int result = NativeMethods.InjectHook(ProcessId);
                if (result != 0)
                    throw new EntryPointNotFoundException("Could not inject process");
                result = NativeMethods.GetState(ProcessId, ref _state, timeout);
                if (result != 0)
                    throw new EntryPointNotFoundException("Could not connect to process");
            });
        }

        public static void UnhookProcess()
        {
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                AsyncHookThread.Current.WaitFor(() => _ = NativeMethods.ReleaseHook(ProcessId));
                disposedValue = true;
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

using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Il2CppToolkit.Injection.Client
{
	internal class AsyncHookThread : IDisposable
	{
		private readonly Thread m_thread;
		private readonly AutoResetEvent m_signal;
		private readonly ConcurrentQueue<Action> m_actionQueue = new();
		private volatile bool m_disposing;
		private volatile bool m_disposed;

		private static AsyncHookThread s_current;
		public static AsyncHookThread Current
		{
			get
			{
				if (s_current == null)
					s_current = new();
				return s_current;
			}
		}

		public static void DisposeCurrent()
		{
			s_current?.Dispose();
			s_current = null;
		}

		public AsyncHookThread()
		{
			m_thread = new Thread(ThreadStart);
			m_signal = new(false);
			m_thread.Start();
		}

		public void Post(Action callback)
		{
			m_actionQueue.Enqueue(callback);
			m_signal.Set();
		}

		public void WaitFor(Action callback)
		{
			WaitFor(() =>
			{
				callback();
				return true;
			});
		}

		public T WaitFor<T>(Func<T> callback)
		{
			AutoResetEvent signal = new(false);
			Exception ex = null;
			T result = default;
			Post(() => {
				try
				{
					result = callback();
				}
				catch (Exception innerEx)
				{
					ex = innerEx;
				}
				finally
				{
					signal.Set();
				}
			});
			signal.WaitOne();
			signal.Dispose();
			if (ex != null)
			{
				throw ex;
			}
			return result;
		}

		private void ThreadStart()
		{
			m_signal.WaitOne();
			while (!m_disposing)
			{
				while (m_actionQueue.TryDequeue(out Action callback))
				{
					callback();
				}
				m_signal.WaitOne();
			}
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!m_disposed)
			{
				if (disposing)
				{
					// net472 compatible clear
#if NET5_0_OR_GREATER
					m_actionQueue.Clear();
#else
					while (m_actionQueue.TryDequeue(out _)) ;
#endif
					m_disposing = true;
					m_signal.Set();
					m_thread.Join();
					m_signal.Dispose();
				}

				m_disposed = true;
			}
		}

		public void Dispose()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
	}
}

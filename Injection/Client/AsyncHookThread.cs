using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;

namespace Il2CppToolkit.Injection.Client
{
	internal class AsyncHookThread : IDisposable
	{
		private readonly Thread m_thread;
		private readonly AutoResetEvent m_signal;
		private readonly ConcurrentQueue<Action> m_actionQueue = new();
		private bool m_disposed;
		private volatile bool m_disposing;

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
			Post(() =>
			{
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
#if NET5_0_OR_GREATER
					m_actionQueue.Clear();
#else
					// net472 compatible clear
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

		~AsyncHookThread()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: false);
		}
	}

	public class AsyncHookThreadDispatcher : IDisposable
	{
		private AsyncHookThread m_thread;
		public AsyncHookThreadDispatcher()
		{
			m_thread = AsyncHookThreadManager.AddRef();
		}
		private bool m_disposed;

		public void Post(Action callback) => m_thread.Post(callback);
		public void WaitFor(Action callback) => m_thread.WaitFor(callback);
		public void WaitFor<T>(Func<T> callback) => m_thread.WaitFor<T>(callback);

		protected virtual void Dispose(bool disposing)
		{
			if (!m_disposed)
			{
				if (disposing)
				{
					AsyncHookThreadManager.Release();
				}

				m_disposed = true;
			}
		}

		~AsyncHookThreadDispatcher()
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

	internal static class AsyncHookThreadManager
	{
		private static AsyncHookThread s_currentThread;
		private static volatile int m_refCount;
		private static readonly object m_lock = new();

		public static AsyncHookThread AddRef()
		{
			lock (m_lock)
			{
				if (m_refCount++ == 0)
				{
					Debug.Assert(s_currentThread == null);
					s_currentThread = new();
				}
			}
			return s_currentThread;
		}

		public static void Release()
		{
			lock (m_lock)
			{
				if (--m_refCount == 0)
				{
					Debug.Assert(s_currentThread != null);
					s_currentThread.Dispose();
					s_currentThread = null;
				}
			}
		}
	}
}

using System;
using System.Diagnostics;
using Il2CppToolkit.Common.Errors;
using Il2CppToolkit.Injection.Client;

namespace Il2CppToolkit.Runtime
{
	public class PinnedReference<T> : IDisposable where T : class, IRuntimeObject
	{
		private uint? Handle;
		private bool IsDisposed;
		private T Reference;
		public T Value
		{
			get
			{
				if (!Handle.HasValue)
					throw new ObjectDisposedException(typeof(PinnedReference<T>).FullName);
				return Reference;
			}
		}

		public PinnedReference(T reference)
		{
			Reference = reference;
			PinObjectMessage response = Reference.Source.ParentContext.InjectionClient.Il2Cpp.PinObject(new()
			{
				Obj = new Il2CppObject()
				{
					Address = Reference.Address
				}
			});
			Handle = response.Obj.Handle;
		}

		public PinnedReference(T reference, uint handle)
		{
			Reference = reference;
			Handle = handle;
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!IsDisposed)
			{
				if (disposing)
				{
					// add explicit Dispose() logic here
				}

				// do this even if Dispose wasn't called to ensure GC
				if (Reference != null && Reference is IDisposable dispRef)
				{
					ErrorHandler.Assert(disposing, $"Object {typeof(T)} is being finalized without calling Dispose!");
					dispRef.Dispose();
				}

				if (Handle.HasValue)
				{
					Reference.Source.ParentContext.InjectionClient.Il2Cpp.FreeObject(new() { Handle = Handle.Value });
				}
				Reference = null;
				Handle = null;
				IsDisposed = true;
			}
		}

		~PinnedReference()
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
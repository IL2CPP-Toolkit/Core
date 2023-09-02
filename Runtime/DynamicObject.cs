using Il2CppToolkit.Injection.Client;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Il2CppToolkit.Runtime
{
	public class DynamicObject<T> : IDisposable
	{
		public IRuntimeObject? Pointer { get; private set; }

		private readonly Il2CsRuntimeContext Runtime;
		private PinnedReference<IRuntimeObject>? PinnedRef;
		private bool IsDisposed;

		public DynamicObject(Il2CsRuntimeContext runtime)
		{
			Runtime = runtime;
		}

		public DynamicObject(T value)
		{
			if (value is not IRuntimeObject runtimeObject)
				throw new InvalidCastException("Cannot create a DynamicObject for a non-IRuntimeObject type");
			Runtime = runtimeObject.Source.ParentContext;
			Pointer = runtimeObject;
			PinnedRef = new(Pointer);
		}

		public void Create(params object[] arguments)
		{
			CreateObjectRequest request = new()
			{
				Klass = Il2CppTypeName<T>.klass
			};
			request.Arguments.AddRange(arguments.Select(Il2CppTypeInfoLookup<T>.ValueFrom));
			CreateObjectResponse response = Runtime.InjectionClient.Il2Cpp.CreateObject(request);
			if (response.ReturnValue.ValueCase != Value.ValueOneofCase.Obj)
			{
				throw new InvalidCastException("Cannot create a DynamicObject for a non-object type");
			}
			Pointer = new ObjectPointer(Runtime, response.ReturnValue.Obj.Address);
			PinnedRef = new(Pointer, response.ReturnValue.Obj.Handle);
		}

		public T Hydrate()
		{
			return (T)Activator.CreateInstance(typeof(T), Runtime, Pointer.Address);
		}

		public U Call<U>(string name, params object[] arguments)
		{
			return Il2CppTypeInfoLookup<T>.CallMethod<U>(Pointer, name, arguments);
		}

		public void Call(string name, params object[] arguments)
		{
			Il2CppTypeInfoLookup<T>.CallMethod(Pointer, name, arguments);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!IsDisposed)
			{
				if (disposing)
				{
					PinnedRef?.Dispose();
					PinnedRef = null;
					Pointer = null;
				}
				IsDisposed = true;
			}
		}

		~DynamicObject()
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

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Il2CppToolkit.ReverseCompiler
{
    public class ArtifactContainer
    {
        private class ArtifactState
        {
            internal static object EmptyValue = new();

            public TaskCompletionSource<object> CompletionSource = new();
            public object Value = EmptyValue;

            public ArtifactState() { }
            public ArtifactState(object value)
            {
                Value = value;
                CompletionSource.TrySetResult(value);
            }
        }

        private Dictionary<IStateSpecification, ArtifactState> m_artifacts = new();

        public void Set<T>(ITypedSpecification<T> spec, T value)
        {
            if (m_artifacts.TryGetValue(spec, out ArtifactState state))
            {
                if (state.Value != ArtifactState.EmptyValue)
                {
                    throw new InvalidOperationException("Value already set");
                }
                state.Value = value;
                state.CompletionSource.TrySetResult(value);
            }
            else
            {
                m_artifacts.Add(spec, new ArtifactState(value));
            }
        }

        public void Set(IStateSpecification spec, object value)
        {
            if (m_artifacts.TryGetValue(spec, out ArtifactState state))
            {
                if (state.Value != ArtifactState.EmptyValue)
                {
                    throw new InvalidOperationException("Value already set");
                }
                state.Value = value;
                state.CompletionSource.TrySetResult(value);
            }
            else
            {
                m_artifacts.Add(spec, new ArtifactState(value));
            }
        }

        public async Task<T> GetAsync<T>(ITypedSpecification<T> spec)
        {
            if (!m_artifacts.TryGetValue(spec, out ArtifactState state))
            {
                state = new ArtifactState();
                m_artifacts.Add(spec, state);
            }

            return (T)(await state.CompletionSource.Task);
        }

        public T Get<T>(ITypedSynchronousState<T> spec)
        {
            if (!m_artifacts.TryGetValue(spec, out ArtifactState state))
            {
                state = new ArtifactState();
                m_artifacts.Add(spec, state);
            }

            return (T)state.Value;
        }

        public bool Has(IStateSpecification spec)
        {
            if (m_artifacts.TryGetValue(spec, out ArtifactState state))
            {
                return state.Value != ArtifactState.EmptyValue;
            }
            return false;
        }
    }
}
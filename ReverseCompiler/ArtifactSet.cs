using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace Il2CppToolkit.ReverseCompiler
{
    internal class ArtifactSet<K, V>
    {
        private Dictionary<K, TaskCompletionSource<V>> m_waiters = new();
        private HashSet<Tuple<K, V>> m_set = new();

        public ArtifactSet()
        {
        }

        public void Add(K key, V value)
        {
            m_set.Add(new Tuple<K, V>(key, value));
            if (m_waiters.TryGetValue(key, out var taskSource))
            {
                taskSource.TrySetResult(value);
            }
        }

        public Task<V> Get(K key)
        {
            return Get<V>(key);
        }

        public async Task<U> Get<U>(K key) where U : V
        {
            var value = m_set.FirstOrDefault(tuple => tuple.Item1.Equals(key));
            if (value != null)
            {
                return (U)value.Item2;
            }
            if (!m_waiters.ContainsKey(key))
            {
                m_waiters.Add(key, new TaskCompletionSource<V>());
            }
            V temp = await m_waiters[key].Task;
            return (U)temp;
        }
    }
}
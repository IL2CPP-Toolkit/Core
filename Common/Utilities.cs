using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Il2CppToolkit.Common
{
    public static class Utilities
    {
        public static ulong GetTypeTag(int discriminator, uint typeToken)
        {
            return ((ulong)discriminator << 32) + typeToken;
        }
        public static ulong GetTypeTag(uint discriminator, uint typeToken)
        {
            return ((ulong)discriminator << 32) + typeToken;
        }
        public static string GetTypeTag(long nameIndex, long namespaceIndex, long typeToken)
        {
            return $"{nameIndex}.{namespaceIndex}.{typeToken}";
        }
    }
    public class MutableTypeListIterator<T> : IEnumerable<T>
    {
        private IList<T> Owner;
        private Queue<T> OpenList;
        public MutableTypeListIterator(IList<T> owner)
        {
            Owner = owner;
            OpenList = new(owner);
        }

        private IEnumerable<T> Iterate()
        {
            while (OpenList.TryDequeue(out T item))
                yield return item;
        }

        public void Add(T item)
        {
            Owner.Add(item);
            OpenList.Append(item);
        }

        public IEnumerator<T> GetEnumerator() => Iterate().GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => Iterate().GetEnumerator();
    }
}

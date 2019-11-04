using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace MimiTools.Enumerables
{
    class SkipWhileOrderedEnumerable<T> : IOrderedEnumerable<T>
    {
        private readonly IEnumerable<T> Enumerable;

        public SkipWhileOrderedEnumerable(IOrderedEnumerable<T> enumerable, Func<T, bool> predicate)
            => Enumerable = enumerable.AsEnumerable().SkipWhile(predicate);

        public IOrderedEnumerable<T> CreateOrderedEnumerable<TKey>(Func<T, TKey> keySelector, IComparer<TKey> comparer, bool descending)
        {
            if (descending)
                return Enumerable.OrderByDescending(keySelector, comparer);
            return Enumerable.OrderBy(keySelector, comparer);
        }

        public IEnumerator<T> GetEnumerator()
            => Enumerable.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => Enumerable.GetEnumerator();
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace MimiTools.Enumerables
{
    class MergedOrderedEnumerable<T> : IOrderedEnumerable<T>
    {
        private readonly IOrderedEnumerable<T> First;
        private readonly IOrderedEnumerable<T> Second;
        private readonly IComparer<T> Comparer;

        public MergedOrderedEnumerable(IOrderedEnumerable<T> first, IOrderedEnumerable<T> second, IComparer<T> comparer = null)
        {
            Comparer = comparer ?? Comparer<T>.Default;
            First = first;
            Second = second;
        }

        IOrderedEnumerable<T> IOrderedEnumerable<T>.CreateOrderedEnumerable<TKey>(Func<T, TKey> keySelector, IComparer<TKey> comparer, bool descending)
            => this.OrderBy(keySelector, Comparer<TKey>.Create((x, y) =>
            {
                if (descending)
                    return comparer.Compare(y, x);
                return comparer.Compare(x, y);
            }));

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
            => new MergeEnumerator(First.GetEnumerator(), Second.GetEnumerator(), Comparer);

        IEnumerator IEnumerable.GetEnumerator()
            => ((IEnumerable<T>)this).GetEnumerator();

        private class MergeEnumerator : IEnumerator<T>
        {
            internal MergeEnumerator(IEnumerator<T> first, IEnumerator<T> second, IComparer<T> comparer)
            {
                First = first;
                Second = second;
                Comparer = comparer;
                FirstHasValue = First.MoveNext();
                SecondHasValue = Second.MoveNext();
            }

            private bool Active = false;
            private bool FirstHasValue = false;
            private bool SecondHasValue = false;

            private readonly IEnumerator<T> First;
            private readonly IEnumerator<T> Second;
            private readonly IComparer<T> Comparer;

            private T _item = default(T);

            public T Current
            {
                get
                {
                    if (!Active)
                        throw new InvalidOperationException();
                    return _item;
                }
            }

            object IEnumerator.Current => Current;

            public void Dispose()
            {
                First.Dispose();
                Second.Dispose();
            }

            public bool MoveNext()
            {
                Active = true;
                if (FirstHasValue && SecondHasValue)
                {
                    if (Comparer.Compare(First.Current, Second.Current) <= 0)
                    {
                        _item = First.Current;
                        FirstHasValue = First.MoveNext();
                    }
                    else
                    {
                        _item = Second.Current;
                        SecondHasValue = Second.MoveNext();
                    }
                }
                else if (FirstHasValue)
                {
                    _item = First.Current;
                    FirstHasValue = First.MoveNext();
                }
                else if (SecondHasValue)
                {
                    _item = Second.Current;
                    SecondHasValue = Second.MoveNext();
                }
                else
                    return false;
                return true;
            }

            public void Reset()
            {
                Active = false;
                First.Reset();
                Second.Reset();
                FirstHasValue = First.MoveNext();
                SecondHasValue = Second.MoveNext();
            }
        }
    }
}

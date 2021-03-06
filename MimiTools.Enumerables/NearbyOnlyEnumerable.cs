﻿using System;
using System.Collections;
using System.Collections.Generic;

namespace MimiTools.Enumerables
{
    class NearbyOnlyEnumerable<T> : IEnumerable<T>
    {
        private readonly IEnumerable<T> _source;
        private readonly Func<T, bool> _filter;
        private readonly int _distance;

        internal NearbyOnlyEnumerable(IEnumerable<T> source, Func<T, bool> filter, int max_distance)
        {
            _source = source;
            _filter = filter;
            _distance = max_distance;
        }

        public IEnumerator<T> GetEnumerator()
            => new RelevantFilter(_source.GetEnumerator(), _filter, _distance);

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();

        private class RelevantFilter : IEnumerator<T>
        {
            internal RelevantFilter(IEnumerator<T> source, Func<T, bool> filter, int max_distance)
            {
                _source = source;
                _filter = filter;
                _array = new T[1 + (max_distance * 2)];
                _ptr = -1;
                _pass_count = 0;
                _passes = new bool[_array.Length];
                _remaining = max_distance + 1;

                for (int i = 0; i < max_distance; i++)
                    Traverse();
            }

            private readonly IEnumerator<T> _source;
            private readonly Func<T, bool> _filter;

            private readonly T[] _array;
            private readonly bool[] _passes;

            private int _ptr;
            private int _pass_count;
            private int _remaining;

            public T Current { get => _array[_ptr]; }

            object IEnumerator.Current => Current;


            private int GetNewest()
                => (_ptr + (_array.Length / 2)) % _array.Length;

            public bool MoveNext()
            {
                do
                    if (!Traverse())
                        return false;
                while (_pass_count == 0);

                return true;
            }

            public void Reset()
                => throw new InvalidOperationException();

            private bool Traverse()
            {
                _ptr = (_ptr + 1) % _array.Length;

                //The newest is also the oldest until we set a new value to it
                int newest = GetNewest();
                if (_passes[newest])
                    _pass_count--;

                if (_source.MoveNext())
                {
                    _array[newest] = _source.Current;
                    if (_passes[newest] = _filter(_array[newest]))
                        _pass_count++;
                }
                else
                    _remaining--;

                return _remaining > 0;
            }

            void IDisposable.Dispose()
                => _source.Dispose();
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace MimiTools.Collections.Weak
{
    public sealed class WeakValueDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IDisposable where TValue : class
    {
        public WeakValueDictionary() : this(EqualityComparer<TKey>.Default) { }

        public WeakValueDictionary(IEqualityComparer<TKey> comparer)
        {
            _buckets = null;
            _comparer = comparer;
            _count = 0;
            _entries = null;
            _freelist = -1;
            _version = 0;
            Resize(5);
        }

        private int[] _buckets;
        private readonly IEqualityComparer<TKey> _comparer;
        private int _count;
        private Entry[] _entries;
        private int _freelist;
        private int _version;

        public ICollection<TKey> Keys => throw new NotImplementedException();

        public ICollection<TValue> Values => throw new NotImplementedException();

        public int Count => _count;

        public bool IsReadOnly => false;

        public TValue this[TKey key]
        {
            get
            {
                int target = Find(key, out _, out _);
                if (target != -1)
                {
                    TValue value = _entries[target].Value;
                    if (value != null)
                        return value;
                }
                throw new KeyNotFoundException();
            }
            set => Set(key, value, false);
        }

        public interface IEntry
        {
            void Clean();
            bool HasCleanup { get; }
            bool IsValid { get; }
            TKey Key { get; set; }
            TValue Value { get; set; }
        }

        public void Clean()
        {
            for (int b = 0; b < _buckets.Length; b++)
            {
                int p = -1; int i = _buckets[b];
                while (i != -1)
                {
                    if (_entries[i].Value == null)
                    {
                        Remove(p, i);
                        if (p == -1)
                            i = _buckets[b];
                        else
                            i = _entries[p].Next;
                    }
                    else
                    {
                        p = i;
                        i = _entries[i].Next;
                    }
                }
            }
        }

        private int ConsumeFreelist()
        {
            int target = _freelist;
            _freelist = _entries[target].Next;
            return target;
        }

        private int Find(TKey key, out int hash, out int prev)
        {
            int h = hash = _comparer.GetHashCode(key);
            int p = -1;
            for (int i = _buckets[(h & 0x7FFFFFFF) % _buckets.Length]; i != -1; i = _entries[i].Next)
            {
                if (_entries[i].Hash == h && _comparer.Equals(key, _entries[i].Key))
                {
                    prev = p;
                    return i;
                }
                p = i;
            }
            prev = p;
            return -1;
        }

        private int GetFreeEntry()
        {
            if (_freelist != -1)
                return ConsumeFreelist();

            Clean();

            if (_freelist == -1)
                Resize(HashHelper.NextPrime(_buckets.Length * 2));

            return ConsumeFreelist();
        }

        private void Remove(int prev, int target)
        {
            int bucket = (_entries[target].Hash & 0x7FFFFFFF) % _buckets.Length;
            if (prev != -1)
                _entries[prev].Next = _entries[target].Next;
            else
                _buckets[bucket] = _entries[target].Next;

            _entries[target].Key = default;
            _entries[target].Value = default;
            _entries[target].Hash = default;

            _entries[target].Next = _freelist;
            _freelist = target;

            _version++;
        }

        private void Resize(int size)
        {
            _count = 0;
            Array.Resize(ref _entries, size);
            Array.Resize(ref _buckets, size);
            for (int i = 0; i < size; i++)
                _buckets[i] = -1;

            _freelist = -1;
            for (int i = 0; i < size; i++)
                if (_entries[i].IsValid)
                {
                    int b = (_entries[i].Hash & 0x7FFFFFFF) % size;
                    _entries[i].Next = _buckets[b];
                    _buckets[b] = i;
                    _count++;
                }
                else
                {
                    _entries[i].Next = _freelist;
                    _freelist = i;
                }
        }

        private void Set(TKey key, TValue value, bool add)
        {
            if (key != null && value != null)
            {
                int target = Find(key, out int hash, out int prev);
                if (target == -1)
                {
                    _count++;
                    target = GetFreeEntry();
                    if (prev != -1)
                        _entries[prev].Next = target;
                    else
                        _buckets[(hash & 0x7FFFFFFF) % _buckets.Length] = target;
                    _entries[target].Key = key;
                    _entries[target].Hash = hash;
                    _entries[target].Next = -1;
                }
                else if (add)
                    throw new ArgumentException("An entry by that key already exists!");

                _entries[target].Value = value;
                _count++;
                _version++;
            }
            else
                throw new ArgumentNullException();
        }

        public void Add(TKey key, TValue value)
            => Set(key, value, true);

        public bool ContainsKey(TKey key)
            => Find(key, out _, out _) != -1;

        public bool Remove(TKey key)
        {
            int target = Find(key, out _, out int prev);
            if (target != -1)
            {
                Remove(prev, target);
                return true;
            }
            return false;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            int target = Find(key, out _, out _);

            if (target != -1)
            {
                value = _entries[target].Value;
                return value != null;
            }

            value = default;
            return false;
        }

        public void Add(KeyValuePair<TKey, TValue> item)
            => Add(item.Key, item.Value);

        public void Clear()
        {
            for (int i = 0; i < _entries.Length; i++)
                _entries[i].Release();

            _buckets = null;
            _count = 0;
            _entries = null;
            _freelist = -1;
            _version = -1;
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            int target = Find(item.Key, out _, out _);
            if (target == -1)
                return false;

            TValue value = _entries[target].Value;
            return item.Value == value;
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            int target = Find(item.Key, out _, out int prev);
            if (target == -1)
                return false;

            if (_entries[target].Value == item.Value)
            {
                Remove(prev, target);
                return true;
            }

            return false;
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();

        private struct Entry
        {
            private GCHandle _value;
            public int Hash;
            public int Next;
            public bool IsValid => Value != null;

            public TKey Key;

            public TValue Value
            {
                get
                {
                    if (_value.IsAllocated)
                        return (TValue)_value.Target;
                    return default;
                }
                set
                {
                    if (_value.IsAllocated)
                        _value.Target = value;
                    else
                        _value = GCHandle.Alloc(value, GCHandleType.Weak);
                }
            }

            public void Release()
            {
                if (_value.IsAllocated)
                    _value.Free();
            }
        }

        #region IDisposable Support

        ~WeakValueDictionary()
            => Clear();

        public void Dispose()
            => Clear();
        #endregion
    }
}

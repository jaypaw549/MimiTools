using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MimiTools.Collections.Weak
{
    public sealed class WeakDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IDisposable where TKey : class where TValue : class
    {
        public WeakDictionary() : this(EqualityComparer<TKey>.Default) { }

        public WeakDictionary(IEqualityComparer<TKey> comparer)
        {
            _buckets = null;
            _comparer = comparer;
            Count = 0;
            _entries = null;
            _freelist = -1;
            _version = 0;
            Resize(5);
        }

        private int[] _buckets;
        private readonly IEqualityComparer<TKey> _comparer;
        private Entry[] _entries;
        private int _freelist;
        private int _version;

        public ICollection<TKey> Keys => throw new NotImplementedException();

        public ICollection<TValue> Values => throw new NotImplementedException();

        public int Count { get; private set; }

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

        public void Clean()
        {
            for (int b = 0; b < _buckets.Length; b++)
            {
                int p = -1;
                int i = _buckets[b];
                while (i != -1)
                {
                    if (_entries[i].Key == null || _entries[i].Value == null)
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
            if (prev != -1)
                _entries[prev].Next = _entries[target].Next;
            else
                _buckets[(_entries[target].Hash & 0x7FFFFFFF) % _buckets.Length] = _entries[target].Next;

            _entries[target].Key = default;
            _entries[target].Value = default;
            _entries[target].Hash = default;
            _entries[target].Next = _freelist;
            _freelist = target;

            _version++;
        }

        private void Resize(int size)
        {
            if (_disposed)
            {
                GC.ReRegisterForFinalize(this);
                _disposed = false;
            }
            Count = 0;
            Array.Resize(ref _entries, size);
            Array.Resize(ref _buckets, size);
            for (int i = 0; i < size; i++)
                _buckets[i] = -1;

            for (int i = 0; i < size; i++)
                if (_entries[i].IsValid)
                {
                    int b = (_entries[i].Hash & 0x7FFFFFFF) % size;
                    _entries[i].Next = _buckets[b];
                    _buckets[b] = i;
                    Count++;
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
                    Count++;
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
                Count++;
                _version++;
            }
            else
                throw new ArgumentNullException(nameof(value));
        }

        public void Add(TKey key, TValue value)
            => Set(key, value, true);

        public bool ContainsKey(TKey key)
            => Find(key, out _, out _) != -1;

        public bool Remove(TKey key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

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
            for (int i = 0; i < (_entries?.Length ?? 0); i++)
                _entries[i].Release();

            _buckets = null;
            Count = 0;
            _entries = null;
            _freelist = -1;
            _version = -1;

            _disposed = true;
            GC.SuppressFinalize(this);
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            int target = Find(item.Key, out _, out _);
            if (target == -1)
                return false;

            TValue value = _entries[target].Value;
            return Equals(value, item.Value);
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

            if (Equals(_entries[target].Value, item.Value))
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
            private GCHandle _key;
            private GCHandle _value;
            public int Hash;
            public int Next;

            public bool IsValid => Key != null && Value != null;

            public TKey Key
            {
                get
                {
                    if (_key.IsAllocated)
                        return (TKey)_key.Target;
                    return default;
                }
                set
                {
                    if (_key.IsAllocated)
                        _key.Target = value;
                    else
                        _key = GCHandle.Alloc(value, GCHandleType.Weak);
                }
            }

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
                if (_key.IsAllocated)
                    _key.Free();

                if (_value.IsAllocated)
                    _value.Free();
            }
        }

        #region IDisposable Support

        private bool _disposed = true;

        ~WeakDictionary()
            => Clear();

        public void Dispose()
            => Clear();
        #endregion
    }
}

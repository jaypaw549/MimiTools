using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace MimiTools.Collections.Unmanaged
{
    public unsafe struct UnmanagedDictionary<TKey, TValue> : IDisposable, IDictionary<TKey, TValue> where TKey : unmanaged where TValue : unmanaged
    {
        private UnmanagedStorage<DictionaryEntry> _entries;
        private UnmanagedHashStructure<DictionaryEntry> _hash;
        private DictionaryEntry* _freelist;

        public ICollection<TKey> Keys => Array.ConvertAll(GetKeyValuePairs(), kvp => kvp.Key);

        public ICollection<TValue> Values => Array.ConvertAll(GetKeyValuePairs(), kvp => kvp.Value);

        public int Count { get; private set; }

        public bool IsReadOnly => false;

        public long MemoryUsage => _entries.MemorySize + _hash.MemorySize;

        public TValue this[TKey key]
        {
            get
            {
                if (GetEntryInfo(key, key.GetHashCode(), out DictionaryEntry* target, out _))
                    return *GetValue(target);
                throw new KeyNotFoundException("No such key was found in this dictionary!");
            }
            set => Alloc(key, value, true);
        }

        public void Add(TKey key, TValue value)
        {
            if (!Alloc(key, value, false))
                throw new InvalidOperationException("Specified key already exists!");
        }

        public void Add(KeyValuePair<TKey, TValue> item)
            => Add(item.Key, item.Value);

        public void Clear()
        {
            _hash.Release();
            _entries.Clear(false);
            Count = 0;
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            if (GetEntryInfo(item.Key, item.Key.GetHashCode(), out DictionaryEntry* target, out _))
            {
                TKey* key = GetKey(target);
                TValue* value = GetValue(target);

                return key->Equals(item.Key) && value->Equals(item.Value);
            }
            return false;
        }

        public bool ContainsKey(TKey key)
            => GetEntryInfo(key, key.GetHashCode(), out _, out _);

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));

            if (Count > array.Length - arrayIndex)
                throw new ArgumentException("Not enough space in target array!");

            KeyValuePair<TKey, TValue>[] data = GetKeyValuePairs();

            for (int i = 0; i < data.Length; i++)
                array[arrayIndex++] = data[i];
        }

        public void Dispose()
            => Clear();

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
            => GetKeyValuePairs().AsEnumerable().GetEnumerator();

        public bool Remove(TKey key)
        {
            if (!GetEntryInfo(key, key.GetHashCode(), out DictionaryEntry* target, out DictionaryEntry* prev))
                return false;

            Clean(target, prev);
            return true;
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            if (GetEntryInfo(item.Key, item.Key.GetHashCode(), out DictionaryEntry* target, out DictionaryEntry* prev))
            {
                TKey* key = GetKey(target);
                TValue* value = GetValue(target);

                if (key->Equals(item.Key) && value->Equals(item.Value))
                {
                    Clean(target, prev);
                    return true;
                }
            }
            return false;
        }

        public void TrimExcess()
        {
            _hash.Release();
            _entries.Compact();
            for (int i = 0; i < _entries.Size; i++)
                _hash.Add(_entries.GetPointer(i));
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            value = default;
            if (!GetEntryInfo(key, key.GetHashCode(), out DictionaryEntry* target, out _))
                return false;

            value = *GetValue(target);
            return true;
        }

        private bool Alloc(TKey key, TValue value, bool set_if_exists)
        {
            int hash = key.GetHashCode();
            if (GetEntryInfo(key, hash, out DictionaryEntry* entry, out _))
            {
                if (set_if_exists)
                {
                    *GetValue(entry) = value;
                    return true;
                }
                return false;
            }

            if (_freelist == null)
                Resize();

            entry = _freelist;
            _freelist = entry->Next;

            entry->Hash = hash;
            entry->IsValid = true;
            _hash.Add(entry);

            *GetKey(entry) = key;
            *GetValue(entry) = value;

            Count++;

            return true;
        }

        private void Clean(DictionaryEntry* target, DictionaryEntry* last)
        {
            _hash.Remove(target, last);

            UnsafeHelper.Zero(target, sizeof(DictionaryEntry) + _entries.Padding);

            target->IsValid = false;
            target->Next = _freelist;
            _freelist = target;

            Count--;
        }

        private bool GetEntryInfo(TKey key, int hash, out DictionaryEntry* target, out DictionaryEntry* last)
        {
            last = null;
            target = _hash.GetBucket(hash);
            while (target != null)
            {
                if (target->IsValid && hash == target->Hash)
                    if (GetKey(target)->Equals(key))
                        return true;

                last = target;
                target = target->Next;
            }
            return false;
        }

        private KeyValuePair<TKey, TValue>[] GetKeyValuePairs()
        {
            UnmanagedStorage<DictionaryEntry> entries = _entries.Clone();
            KeyValuePair<TKey, TValue>[] pairs;
            try
            {
                entries.Compact();
                pairs = new KeyValuePair<TKey, TValue>[entries.Size];
                for (int i = 0; i < entries.Size; i++)
                {
                    DictionaryEntry* entry = entries.GetPointer(i);
                    pairs[i] = new KeyValuePair<TKey, TValue>(entry->key, entry->value);
                }
            }
            finally
            {
                entries.Clear(false);
            }
            return pairs;
        }

        private void Resize()
        {
            int size = _entries.Size * 2;
            if (size < 5)
                size = 5;

            //_entries.SetPadding(sizeof(TKey) + sizeof(TValue));

            _hash.Release();
            _entries.Resize(size);
            _hash.Alloc(HashHelper.NextPrime(size));

            for (int i = 0; i < _entries.Size; i++)
            {
                DictionaryEntry* entry = _entries.GetPointer(i);
                if (entry->IsValid)
                    _hash.Add(entry);
                else
                {
                    entry->Next = _freelist;
                    _freelist = entry;
                }
            }
        }

        private static TKey* GetKey(DictionaryEntry* entry)
            => &entry->key;

        private static TValue* GetValue(DictionaryEntry* entry)
            => &entry->value;

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();

        private struct DictionaryEntry : IUnmanagedHashable<DictionaryEntry>, IUnmanagedMoveable<DictionaryEntry>
        {
            public int Hash { get; set; }

            public unsafe DictionaryEntry* Next { get; set; }

            public bool IsValid { get; set; }

            internal TKey key;
            internal TValue value;

            public unsafe void MassMoved(long offset_in_bytes)
            {
                Next = (DictionaryEntry*)((byte*)Next + offset_in_bytes);
            }

            /// <summary>
            /// If we're moved, we'll get rehashed, so just invalidate the previous block.
            /// </summary>
            /// <param name="src"></param>
            public unsafe void OnMoved(DictionaryEntry* src) { src->IsValid = false; }

            public unsafe void OnMoving(DictionaryEntry* dst) { }
        }
    }
}

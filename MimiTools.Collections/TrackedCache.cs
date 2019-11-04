using MimiTools.Extensions.IComparer;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace MimiTools.Collections
{
    public class TrackedCache<TKey, TValue> : IDictionary<TKey, TValue>
    {
        public TrackedCache(int cache_size, int tracker_size)
        {
            if (tracker_size < 0)
                throw new ArgumentOutOfRangeException(nameof(tracker_size), "Must be a positive number!");
            if (tracker_size < cache_size)
                throw new ArgumentOutOfRangeException(nameof(tracker_size), "Cannot be lower than Cache Size!");
            if (cache_size <= 0)
                throw new ArgumentOutOfRangeException(nameof(cache_size), "Must be a positive number!");

            CacheSize = cache_size;
            TrackerSize = tracker_size;

            Comparer = Comparer<TKey>.Create(CompareEntries);
            CacheHeap = new TouchableHeap<TKey>(Comparer.Invert());
            TrackerHeap = new TouchableHeap<TKey>(Comparer.Invert());
        }

        private Dictionary<TKey, CacheEntry> CacheMap = new Dictionary<TKey, CacheEntry>();
        public int CacheSize { get; private set; }
        private readonly IComparer<TKey> Comparer;
        private readonly TouchableHeap<TKey> CacheHeap;
        private readonly TouchableHeap<TKey> TrackerHeap;
        public int TrackerSize { get; private set; }

        ICollection<TKey> IDictionary<TKey, TValue>.Keys => CacheHeap.ToList();

        ICollection<TValue> IDictionary<TKey, TValue>.Values => CacheHeap.Select(key => CacheMap[key].Value).ToList();

        int ICollection<KeyValuePair<TKey, TValue>>.Count => CacheHeap.Count;

        bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => false;

        TValue IDictionary<TKey, TValue>.this[TKey key]
        {
            get
            {
                if (TryGetEntry(key, out TValue value))
                    return value;
                return default;
            }
            set => TrySetEntry(key, value);
        }

        public void Clear()
        {
            CacheHeap.Clear();
            TrackerHeap.Clear();
            CacheMap.Clear();
        }

        public TValue GetOrCreateEntry(TKey key, Func<TKey, TValue> func_value)
        {
            if (TryGetEntry(key, out TValue value))
                return value;

            value = func_value(key);

            TrySetEntry(key, value);

            return value;
        }

        public void ResizeCache(int cache, int tracker)
        {
            if (tracker < 0)
                throw new ArgumentOutOfRangeException(nameof(tracker), "Must be a positive number!");
            if (tracker < cache)
                throw new ArgumentOutOfRangeException(nameof(tracker), "Cannot be lower than Cache Size!");
            if (cache <= 0)
                throw new ArgumentOutOfRangeException(nameof(cache), "Must be a positive number!");

            CacheSize = cache;
            TrackerSize = tracker;
            while (CacheSize < CacheHeap.Count)
            {
                CacheEntry entry = CacheMap[CacheHeap.RemovePriority()];
                entry.HasValue = false;
                entry.Value = default;
            }

            while (TrackerSize < TrackerHeap.Count)
                CacheMap.Remove(TrackerHeap.RemovePriority());
        }

        public bool TryGetEntry(TKey key, out TValue value)
        {
            value = default;
            if (!CacheMap.TryGetValue(key, out CacheEntry entry))
            {
                CacheMap[key] = entry = new CacheEntry()
                {
                    AccessAttempts = 1,
                    HasValue = false,
                    LastAccessed = DateTimeOffset.Now,
                    Value = default
                };
                TrackerHeap.Add(key);

                if (TrackerHeap.Count > TrackerSize)
                    CacheMap.Remove(TrackerHeap.RemovePriority());

                return false;
            }

            entry.LastAccessed = DateTimeOffset.Now;
            entry.AccessAttempts++;
            CacheHeap.Touch(key);
            TrackerHeap.Touch(key);

            if (entry.HasValue)
            {
                value = entry.Value;
                return true;
            }

            return false;
        }

        public bool TryPurgeEntry(TKey key)
        {
            bool success = CacheHeap.Remove(key);
            success |= TrackerHeap.Remove(key);
            success |= CacheMap.Remove(key);
            return success;
        }

        public bool TryRemoveEntry(TKey key, out TValue value)
        {
            value = default;

            if (CacheHeap.Remove(key))
            {
                CacheEntry e = CacheMap[key];
                value = e.Value;
                e.HasValue = false;
                e.Value = default;
                TrackerHeap.Touch(key);
                return true;
            }
            return false;
        }

        public bool TrySetEntry(TKey key, TValue value)
        {
            if (!CacheMap.TryGetValue(key, out CacheEntry entry))
            {
                CacheMap[key] = entry = new CacheEntry()
                {
                    AccessAttempts = 1,
                    HasValue = true,
                    LastAccessed = DateTimeOffset.Now,
                    Value = value
                };

                TrackerHeap.Add(key);
                if (TrackerHeap.Count > TrackerSize)
                    CacheMap.Remove(TrackerHeap.RemovePriority());
            }
            else
            {
                entry.LastAccessed = DateTimeOffset.Now;
                entry.AccessAttempts++;
                entry.Value = value;
                entry.HasValue = true;

                if (CacheHeap.Contains(key))
                {
                    CacheHeap.Touch(key);
                    TrackerHeap.Touch(key);
                    return true;
                }
            }

            if (CacheHeap.Count >= CacheSize && Comparer.Compare(CacheHeap.Peek(), key) > 0)
            {
                entry.HasValue = false;
                entry.Value = default;
                TrackerHeap.Touch(key);
                return false;
            }

            CacheHeap.Add(key);
            TrackerHeap.Touch(key);

            if (CacheHeap.Count > CacheSize)
            {
                CacheEntry bad = CacheMap[CacheHeap.RemovePriority()];
                bad.HasValue = false;
                bad.Value = default;
            }

            return entry.HasValue;
        }

        private int CompareEntries(TKey x, TKey y)
        {
            if (x == null)
                throw new ArgumentNullException(nameof(x));
            if (y == null)
                throw new ArgumentNullException(nameof(y));

            bool x_success = CacheMap.TryGetValue(x, out CacheEntry x_value);
            bool y_success = CacheMap.TryGetValue(y, out CacheEntry y_value);

            if (x_success && y_success)
                return x_value.CompareTo(y_value);
            else if (x_success)
                return 1;
            else if (y_success)
                return -1;
            return 0;
        }

        bool IDictionary<TKey, TValue>.ContainsKey(TKey key)
            => CacheHeap.Contains(key);

        void IDictionary<TKey, TValue>.Add(TKey key, TValue value)
            => TrySetEntry(key, value);

        bool IDictionary<TKey, TValue>.Remove(TKey key)
            => TryRemoveEntry(key, out TValue value);

        bool IDictionary<TKey, TValue>.TryGetValue(TKey key, out TValue value)
            => TryGetEntry(key, out value);

        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
            => TrySetEntry(item.Key, item.Value);

        void ICollection<KeyValuePair<TKey, TValue>>.Clear()
            => Clear();

        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
        {
            if (TryGetEntry(item.Key, out TValue value))
                return item.Value.Equals(value);
            return false;
        }

        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            Heap<TKey> clone = CacheHeap.ToMaxHeap(Comparer);
            while (clone.Count > 0)
            {
                TKey key = clone.RemovePriority();
                array[arrayIndex++] = new KeyValuePair<TKey, TValue>(key, CacheMap[key].Value);
            }
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
        {
            if ((this as ICollection<KeyValuePair<TKey, TValue>>).Contains(item))
                return TryRemoveEntry(item.Key, out var nothing);
            return false;
        }

        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
            => CacheHeap.Select(key => new KeyValuePair<TKey, TValue>(key, CacheMap[key].Value)).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => (this as IEnumerable<KeyValuePair<TKey, TValue>>).GetEnumerator();

        private class CacheEntry : IComparable<CacheEntry>
        {
            internal int AccessAttempts;
            internal bool HasValue;
            internal DateTimeOffset LastAccessed;
            internal TValue Value;

            public int CompareTo(CacheEntry other)
            {
                if (HasValue && !other.HasValue)
                    return 1;
                else if (!HasValue && other.HasValue)
                    return -1;

                return LastAccessed.AddTicks(AccessAttempts * 1000).CompareTo(other.LastAccessed.AddTicks(other.AccessAttempts * 1000));
            }

        }
    }
}

﻿using System;

namespace MimiTools.Collections.Unmanaged
{
    public unsafe struct UnmanagedHashStructure<T> where T : unmanaged, IUnmanagedHashItem<T>
    {
        private T** _buckets;
        private int _count;

        public int Buckets { get => _count; }

        public bool Add(T* target)
        {
            if (_buckets == null)
                return false;

            int bucket = GetIndex(target->Hash);
            for (T* current = _buckets[bucket]; current != null; current = current->Next)
                if (current == target)
                    return false;

            target->Next = _buckets[bucket];
            _buckets[bucket] = target;
            return true;
        }

        public int AddAll(T* target, int count)
        {
            int success = 0;
            for (int i = 0; i < count; i++)
                if (Add(target + i))
                    success++;

            return success;
        }

        public bool Alloc(int count)
        {
            if (_buckets != null)
                return false;

            int block_size = count * sizeof(T*);

            _buckets = (T**)UnsafeHelper.Alloc(block_size);
            GC.AddMemoryPressure(block_size);

            UnsafeHelper.Zero(_buckets, block_size);

            _count = count;
            return true;
        }

        public T* GetBucket(int hash)
        {
            if (_buckets == null)
                return null;
            return _buckets[GetIndex(hash)];
        }

        public bool Release()
        {
            if (_buckets == null)
                return false;

            UnsafeHelper.Free(_buckets);
            GC.RemoveMemoryPressure(_count * sizeof(T**));

            _count = 0;
            _buckets = null;
            return true;
        }

        public bool Remove(T* target, T* prev = null)
        {
            if (_buckets == null)
                return false;

            if (prev != null && prev->Next == target)
            {
                prev->Next = target->Next;
                return true;
            }

            int bucket = GetIndex(target->Hash);
            if (_buckets[bucket] == target)
            {
                _buckets[bucket] = target->Next;
                return true;
            }

            for (T* current = _buckets[bucket]; current->Next != null; current = current->Next)
                if (current->Next == target)
                {
                    current->Next = target->Next;
                    return true;
                }

            return false;
        }

        public int RemoveAll(T* target, int count)
        {
            int success = 0;
            for (int i = 0; i < count; i++)
                if (Remove(target + i))
                    success++;

            return success;
        }

        private int GetIndex(int hash)
            => (hash & 0x7FFFFFFF) % _count;
    }
}

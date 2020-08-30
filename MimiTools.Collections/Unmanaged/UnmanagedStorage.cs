using System;

namespace MimiTools.Collections.Unmanaged
{
    public unsafe struct UnmanagedStorage<T> : IDisposable where T : unmanaged, IUnmanagedMoveable<T>
    {
        public readonly ref T this[int index]
            => ref *GetPointer(index);

        public readonly long this[T* val]
        {
            get
            {
                Validate(val, false);
                return ((byte*)val - _start) / Block;
            }
        }

        private readonly long Block => _padding + sizeof(T);

        public readonly bool Initialized => _start != null;

        public readonly long Padding => _padding;
        public readonly void* Start => _start;
        public readonly int Size => _size;

        public readonly long MemorySize => (sizeof(T) +_padding) * _size;

        private long _padding;
        private int _size;
        private byte* _start;

        public void Clear(bool erase)
        {
            if (_start == null)
                return;

            if (erase)
                UnsafeHelper.Zero(_start, _size * Block);

            UnsafeHelper.Free(_start);
            AdjustMemoryPressure(_size, 0);

            _size = 0;
            _start = null;
        }

        public readonly UnmanagedStorage<T> Clone()
        {
            UnmanagedStorage<T> clone = new UnmanagedStorage<T>();

            byte* alloc = (byte*)UnsafeHelper.Alloc(MemorySize);
            UnsafeHelper.Copy(_start, alloc, MemorySize);
            AdjustMemoryPressure(0, MemorySize);

            for (int i = 0; i < _size; i++)
            {
                T* ptr = IncrementPtr(alloc, i);
                if (ptr->IsValid)
                    ptr->MassMoved(alloc - _start);
            }

            clone._padding = _padding;
            clone._start = alloc;
            clone._size = _size;

            return clone;
        }

        public void Compact()
            => Resize(CompactData(), false);

        public void Dispose()
            => Clear(false);

        public readonly T[] GetData()
        {
            long count = 0;

            long index = 0;
            T[] data = new T[count];
            fixed (T* d = data)
                for (int i = 0; i < _size; i++)
                    d[index++] = this[i];

            return data;
        }

        public readonly byte[][] GetDataWithPadding()
        {
            long block = Block;
            byte[][] data = new byte[_size][];
            for (int i = 0; i < _size; i++)
            {
                byte[] sub_data = new byte[block];
                fixed (byte* b = sub_data)
                    UnsafeHelper.Copy(GetPointer(i), b, block);
                data[i] = sub_data;
            }

            return data;
        }

        public readonly long GetIndex(ref T val)
        {
            fixed (T* v = &val)
                return this[v];
        }

        public readonly T* GetPointer(int index)
        {
            T* ptr = (T*)(_start + (index * Block));
            Validate(ptr, false);
            return ptr;
        }

        public readonly byte[] GetRawData()
        {
            byte[] data = new byte[Block * _size];
            fixed (byte* b = data)
                UnsafeHelper.Copy(_start, b, data.LongLength);
            return data;
        }

        public readonly void Move(int src, int dst, int len, bool overwrite)
        {
            if (src < 0 || src > _size)
                throw new ArgumentOutOfRangeException(nameof(src));

            if (dst < 0 || dst > _size)
                throw new ArgumentOutOfRangeException(nameof(dst));

            if (src + len > _size || dst + len > _size)
                throw new ArgumentOutOfRangeException(nameof(len));

            if (src == dst)
                return;

            if (!overwrite)
                for (int i = dst; i < dst + len; i++)
                    if (GetPointer(i)->IsValid)
                        throw new InvalidOperationException("Blocks in the destination would be overwritten!");

            if (src < dst)
                Move(src + len - 1,
                    dst + len - 1,
                    len,
                    -1);
            else
                Move(src, dst, len, 1);
        }

        public void Resize(int new_size)
            => Resize(new_size, true);

        public bool SetPadding(long padding)
        {
            if (_start != null)
                return false;

            _padding = padding;
            return true;
        }

        public readonly void Validate(T* target, bool null_passes)
        {
            if (null_passes && target == null)
                return;

            if (target < _start || target >= _start + (_size * Block))
                throw new IndexOutOfRangeException(nameof(target));

            if (((byte*)target - _start) % Block != 0)
                throw new IndexOutOfRangeException("Bad alignment!");
        }

        private readonly void AdjustMemoryPressure(long before, long after)
        {
            if (before > after)
                GC.RemoveMemoryPressure((before - after) * Block);
            else if (after > before)
                GC.AddMemoryPressure((after - before) * Block);
        }

        private int CompactData()
        {
            long block = Block;
            T* free = (T*)_start;
            T* end = IncrementPtr(free, _size);
            int count = 0;
            for (T* current = (T*)_start; current < end; current = IncrementPtr(current, 1))
                if (current->IsValid)
                {
                    Move(current, free, block);
                    free = IncrementPtr(free, 1);
                    count++;
                }

            return count;

            static void Move(T* src, T* dst, long block)
            {
                if (src == dst)
                    return;

                src->OnMoving(dst);
                UnsafeHelper.Copy(src, dst, block);
                dst->OnMoved(src);
            }
        }

        private readonly T* IncrementPtr(void* target, int count)
            => (T*)((byte*)target + (count * Block));
        
        private readonly void Move(int src, int dst, int len, int inc)
        {
            T* d_ptr = GetPointer(dst);
            T* s_ptr = GetPointer(src);
            long block = Block;
            for(int i = 0; i < len; i++)
            {
                s_ptr->OnMoving(d_ptr);
                UnsafeHelper.Copy(s_ptr, d_ptr, block);
                UnsafeHelper.Zero(s_ptr, block);
                d_ptr->OnMoved(s_ptr);

                d_ptr = IncrementPtr(d_ptr, inc);
                s_ptr = IncrementPtr(s_ptr, inc);
            }
        }

        private void Resize(int new_size, bool do_size_checks)
        {
            long block = Block;
            if (_start != null)
            {
                if (do_size_checks && new_size < _size && CompactData() > new_size)
                    throw new InvalidOperationException("There are too many items in this collection to resize to that small!");

                byte* before = _start;
                byte* after;
                
                if (new_size > 0)
                    after = (byte*)UnsafeHelper.Realloc(before, new_size * block);
                else
                {
                    UnsafeHelper.Free(before);
                    after = null;
                }

                AdjustMemoryPressure(_size, new_size);
                UnsafeHelper.Zero(after + (_size * block), (new_size - _size) * block);

                if (after != before)
                    for (int i = 0; i < new_size; i++)
                    {
                        T* current = IncrementPtr(after, i);
                        if (current->IsValid)
                            current->MassMoved(after - before);
                    }

                _start = after;
                _size = new_size;
            }
            else
            {
                if (new_size == 0)
                    return;

                _start = (byte*)UnsafeHelper.Alloc(new_size * block);
                AdjustMemoryPressure(0, new_size);

                _size = new_size;
                UnsafeHelper.Zero(_start, new_size * block);
            }
        }
    }
}

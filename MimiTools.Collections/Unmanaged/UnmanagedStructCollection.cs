using System;

namespace MimiTools.Collections.Unmanaged
{
    public unsafe struct UnmanagedStructCollection<T> where T : unmanaged, IUnmanagedMoveable<T>
    {
        public ref T this[long index]
            => ref *GetPointer(index);

        public long this[T* val]
        {
            get
            {
                Validate(val, false);
                return ((byte*)val - _start) / Block;
            }
        }

        private long Block { get => _padding + sizeof(T); }
        public long Padding { get => _padding; }
        public void* Start { get => _start; }
        public int Size { get => _size; }

        private long _padding;
        private int _size;
        private byte* _start;

        public void Clear(bool erase)
        {
            if (erase)
                UnsafeHelper.Zero(_start, _size * Block);

            UnsafeHelper.Free(_start);
            AdjustMemoryPressure(_size, 0);

            _size = 0;
            _start = null;
        }

        public UnmanagedStructCollection<T> Clone()
        {
            UnmanagedStructCollection<T> clone = new UnmanagedStructCollection<T>();

            byte* alloc = (byte*)UnsafeHelper.Alloc(_size * Block);
            UnsafeHelper.Copy(Start, alloc, _size * Block);

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

        public T[] GetData()
        {
            long count = 0;

            long index = 0;
            T[] data = new T[count];
            fixed (T* d = data)
                for (long l = 0; l < _size; l++)
                    d[index++] = this[l];

            return data;
        }

        public byte[][] GetDataWithPadding()
        {
            long block = Block;
            byte[][] data = new byte[_size][];
            for (long l = 0; l < _size; l++)
            {
                byte[] sub_data = new byte[block];
                fixed (byte* b = sub_data)
                    UnsafeHelper.Copy(GetPointer(l), b, block);
                data[l] = sub_data;
            }

            return data;
        }

        public long GetIndex(ref T val)
        {
            fixed (T* v = &val)
                return this[v];
        }

        public T* GetPointer(long index)
            => (T*)(_start + (index * Block));

        public byte[] GetRawData()
        {
            byte[] data = new byte[Block * _size];
            fixed (byte* b = data)
                UnsafeHelper.Copy(_start, b, data.LongLength);
            return data;
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

        public void Validate(T* target, bool null_passes)
        {
            if (null_passes && target == null)
                return;

            if (target < _start || target >= _start + (_size * Block))
                throw new IndexOutOfRangeException(nameof(target));

            if (((byte*)target - _start) % Block != 0)
                throw new IndexOutOfRangeException("Bad alignment!");
        }

        private void AdjustMemoryPressure(long before, long after)
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
                    Move(current, free);
                    free = IncrementPtr(free, 1);
                    count++;
                }

            return count;

            void Move(T* src, T* dst)
            {
                if (src == dst)
                    return;

                UnsafeHelper.Copy(src, dst, block);
                dst->Moved(src, dst);
            }
        }

        private T* IncrementPtr(void* target, long count)
            => (T*)((byte*)target + (count * Block));

        private void Resize(int new_size, bool do_size_checks)
        {
            long block = Block;
            if (_start != null)
            {
                if (do_size_checks && new_size < _size && CompactData() > new_size)
                    throw new InvalidOperationException("There are too many items in this collection to resize to that small!");

                byte* before = _start;
                byte* after = (byte*)UnsafeHelper.Realloc(before, new_size * block);

                AdjustMemoryPressure(_size, new_size);
                UnsafeHelper.Zero(after + (_size * block), (new_size - _size) * block);

                //if (!UnsafeDebugHelper.GetBackend(after).Skip((int) block * _size).All(b => b == 0))
                //    throw new Exception("Failed to zero out data!");

                if (after != before)
                    for (int i = 0; i < new_size; i++)
                    {
                        T* current = IncrementPtr(after, i);
                        if (current->IsValid)
                            current->MassMoved(after - before);
                    }

                _start = (byte*)after;
                _size = new_size;
            }
            else
            {
                _start = (byte*)UnsafeHelper.Alloc(new_size * block);
                AdjustMemoryPressure(0, new_size);

                _size = new_size;
                UnsafeHelper.Zero(_start, new_size * block);
                //if (!Array.TrueForAll(UnsafeDebugHelper.GetBackend(_start), b => b == 0))
                //    throw new Exception("Failed to zero out data!");
            }
        }
    }
}

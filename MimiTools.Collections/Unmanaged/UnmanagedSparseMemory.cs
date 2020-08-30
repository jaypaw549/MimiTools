using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace MimiTools.Collections.Unmanaged
{
    public unsafe struct UnmanagedSparseMemory<T> : IDisposable where T : unmanaged
    {
        private const int default_alloc_size = 1024;
        private int alloc_size;

        public UnmanagedSparseMemory(int alloc_size)
        {
            this.alloc_size = alloc_size;

            free = null;
            index = new UnmanagedHashStructure<BlockHeader>();
            storage = new UnmanagedStorage<BlockHeader>();
            virtual_count = 0;

            Resize(false);
        }

        private BlockHeader* free;
        private UnmanagedHashStructure<BlockHeader> index;
        private UnmanagedStorage<BlockHeader> storage;
        private int virtual_count;

        public readonly long MemoryPerBlock => sizeof(BlockHeader) + alloc_size;

        public readonly long PhysicalMemorySize => storage.MemorySize + index.MemorySize;

        public readonly long VirtualMemorySize => virtual_count * sizeof(T);

        public ref T this[int i] 
        {
            get
            {
                CalculateLocation(i, out int block, out int offset);
                return ref *(T*)(GetMemory(block, true) + offset);
            }
        }

        public byte[] AsContiguousData()
        {
            byte[] data = new byte[VirtualMemorySize];
            fixed (byte* b = data)
                WriteOut(b);
            return data;
        }

        public byte[] AsData(bool sparse)
        {
            if (sparse)
                return AsSparseData();
            return AsContiguousData();
        }

        public byte[] AsSparseData()
        {
            byte[] data = new byte[(sizeof(int) + alloc_size) * storage.Size];
            fixed (byte* b = data)
                for(int i = 0; i < storage.Size; i++)
                {
                    BlockHeader* header = storage.GetPointer(i);
                    *(int*)b = header->index;
                    UnsafeHelper.Copy(GetMemory(header), b + sizeof(int), alloc_size);
                }
            return data;
        }

        public void Dispose()
            => Release(false);

        public T Get(int i)
        {
            T data = default;
            Read(i, &data, 1);
            return data;
        }

        public void Get(int i, out T data)
        {
            fixed (T* ptr = &data)
                Read(i, ptr, 1);
        }

        public void Minimize(bool dealloc)
        {
            if (dealloc)
                for (int i = 0; i < storage.Size; i++)
                    if (UnsafeHelper.IsDefault(GetMemory(storage.GetPointer(i)), alloc_size))
                        storage[i].index = -1;

            Resize(true);
        }

        public void Read(int i, in Span<T> dest)
        {
            fixed (T* ptr = &MemoryMarshal.GetReference(dest))
                Read(i, ptr, dest.Length);
        }

        public void Set(int i, T data)
            => Write(i, &data, 1);

        public void Set(int i, in T data)
        {
            fixed (T* ptr = &data)
                Write(i, ptr, 1);
        }

        public void Write(int i, in Span<T> dest)
        {
            fixed (T* ptr = dest)
                Write(i, ptr, dest.Length);
        }

        public T[] ToArray()
        {
            T[] ret = new T[virtual_count];
            fixed (T* ptr = ret)
                WriteOut((byte*)ptr);
            return ret;
        }

        private byte* Alloc(int i)
        {
            if (free == null)
                Resize(false);

            BlockHeader* entry = free;
            free = entry->Next;

            entry->index = i;
            index.Add(entry);

            return GetMemory(entry);
        }

        private void CalculateLocation(int target, out int block, out int offset)
        {
            long virtual_location = (long) target * sizeof(T);
            block = (int)(virtual_location / alloc_size);
            offset = (int)(virtual_location % alloc_size);
        }

        private void Init()
        {
            alloc_size = default_alloc_size;
        }

        private void Read(int start, T* data, int count)
        {
            if (alloc_size == 0)
                Init();

            if (start + count > virtual_count)
                virtual_count = start + count;

            byte* dst = (byte*)data;

            int offset = start * sizeof(T);
            int index = offset / alloc_size;
            int ptr = offset % alloc_size;

            int remaining = sizeof(T) * count;
            while (remaining > 0)
            {
                byte* src = GetMemory(index++, false);

                int pass = alloc_size - ptr;
                if (pass > remaining)
                    pass = remaining;

                if (src == null)
                    UnsafeHelper.Zero(dst, pass);
                else
                    UnsafeHelper.Copy(&src[ptr], dst, pass);

                dst += pass;
                remaining -= pass;
                ptr = 0;
            }
        }

        private byte* GetMemory(BlockHeader* data)
            => (byte*)(data+1);

        private byte* GetMemory(int i, bool create)
        {
            BlockHeader* entry = index.GetBucket(i);
            while (entry != null && entry->index != i)
                entry = entry->Next;

            if (entry != null && entry->index == i)
                return GetMemory(entry);

            if (create)
                return Alloc(i);

            return null;
        }

        public void Release(bool erase)
        {
            free = null;
            index.Release();
            storage.Clear(erase);
            virtual_count = 0;
        }

        private void Resize(bool minimize)
        {
            if (alloc_size == default)
                alloc_size = default_alloc_size;

            if (!storage.Initialized)
                storage.SetPadding(alloc_size);

            index.Release();
            int prev_size = storage.Size;

            free = null;

            if (minimize)
                storage.Compact();
            else
            {
                int size = storage.Size * 2;
                if (size == 0)
                    size = 1;
                storage.Resize(size);
            }

            index.Alloc(HashHelper.NextPrime(storage.Size));

            for (int i = 0; i < prev_size; i++)
            {
                BlockHeader* entry = storage.GetPointer(i);
                if (entry->IsValid)
                    index.Add(entry);
                else
                {
                    entry->Next = free;
                    free = entry;
                }
            }

            for (int i = prev_size; i < storage.Size; i++)
            {
                storage[i].Next = free;
                free = storage.GetPointer(i);
                storage[i].index = -1;
            }
        }
        
        private void Write(int start, T* data, int count)
        {
            if (alloc_size == 0)
                Init();

            byte* src = (byte*)data;

            if (start + count > virtual_count)
                virtual_count = start + count;

            CalculateLocation(start, out int index, out int ptr);

            int remaining = sizeof(T) * count;

            while (remaining > 0)
            {
                byte* dst = GetMemory(index++, true);

                int pass = alloc_size - ptr;

                if (pass > remaining)
                    pass = remaining;

                UnsafeHelper.Copy(src, &dst[ptr], pass);

                src += pass;
                remaining -= pass;
                ptr = 0;
            }
        }

        private void WriteOut(byte* b)
        {
            for (int i = 0; i < storage.Size; i++)
            {
                BlockHeader* header = storage.GetPointer(i);
                byte* start = b + (header->index * alloc_size);
                UnsafeHelper.Copy(GetMemory(header), start, alloc_size);
            }
        }

        private struct BlockHeader : IUnmanagedHashable<BlockHeader>, IUnmanagedMoveable<BlockHeader>
        {
            internal int index;

            public BlockHeader* Next { get; set; }

            public int Hash => index;

            public bool IsValid => index >= 0;

            public void MassMoved(long offset_in_bytes)
            {
                Next = (BlockHeader*)(((byte*)Next) + offset_in_bytes);
            }

            public void OnMoving(BlockHeader* dst) { }

            public void OnMoved(BlockHeader* src) { }
        }
    }
}
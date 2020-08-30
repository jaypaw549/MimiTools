using System;
using System.Collections.Generic;
using System.Text;

namespace MimiTools.Collections.Unmanaged
{
    public struct UnmanagedArrayDeque<T> : IDisposable where T : unmanaged
    {
        private int _bot;
        private int _top;
        private UnmanagedStorage<Entry> _entries;

        public int Count
        {
            get
            {
                if (_entries.Size > 0)
                    return ((_entries.Size + _bot) - (_top + 1)) % _entries.Size;
                return 0;
            }
        }

        public long MemoryUsage => _entries.MemorySize;

        public readonly ref T this[int index]
            => ref GetReference(index);

        public void Clear(bool erase)
            => _entries.Clear(erase);

        public void Dispose()
            => _entries.Dispose();

        public void AddFirst(in T item)
            => Add(in item, ref _top, _bot, -1);

        public void AddLast(in T item)
            => Add(in item, ref _bot, _top, 1);

        public readonly ref T GetReference(int index)
        {
            if (index > _entries.Size)
                throw new IndexOutOfRangeException("Specified index is out of range!");

            ref Entry entry = ref _entries[(index + _top) % _entries.Size];

            if (entry.IsValid)
                return ref entry.value;

            throw new IndexOutOfRangeException("Specified index is out of range!");
        }

        public readonly ref T PeekFirst()
            => ref GetReference(0);

        public T RemoveFirst()
            => Remove(ref _top, _bot, -1);

        public T RemoveLast()
            => Remove(ref _bot, _top, 1);

        private void Add(in T item, ref int loc, int end, int inc)
        {
            if (loc + inc == end)
                Resize();

            int target = loc = (loc + inc + _entries.Size) % _entries.Size;

            _entries[target].value = item;
            _entries[target].IsValid = true;
        }

        private void Init()
        {
            _bot = 5;
            _top = -1;
            _entries.Resize(5);
        }

        private T Remove(ref int loc, int end, int dec)
        {
            int target = loc;

            if (!_entries[target].IsValid)
                throw new InvalidOperationException(nameof(UnmanagedArrayDeque<T>) + " is empty!");

            loc = (loc + _entries.Size - dec) % _entries.Size;

            _entries[target].IsValid = false;
            return _entries[target].value;
        }

        private void Resize()
        {
            if (!_entries.Initialized)
            {
                Init();
                return;
            }

            int prev = _entries.Size;
            _entries.Resize(Math.Max(5, _entries.Size * 2));

            if (_bot < _top) //If we have a hole in the middle, then we need to move stuff, otherwise it's fine as-is
            {
                int len = prev - _top;
                int new_top = _entries.Size - len;
                _entries.Move(_top, new_top, len, false);
                _top = new_top;
            }
        }

        private struct Entry : IUnmanagedMoveable<Entry>
        {
            internal T value;

            public bool IsValid { get; set; }

            public void MassMoved(long offset_in_bytes) { }

            public unsafe void OnMoved(Entry* src) { src->IsValid = false; } //Just to make sure

            public unsafe void OnMoving(Entry* dst) { }
        }
    }
}

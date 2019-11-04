using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace MimiTools.Data.Entities
{
    public readonly struct DataLayout<TPointer> : IBlockEntity where TPointer : unmanaged, IPointer<TPointer>
    {
        public DataLayout(in Block block)
        {
            _provider = block.Provider;
            _start = block.Start;
            _length = block.Length;
        }

        private readonly DataProvider _provider;
        private readonly long _start;
        private readonly long _length;

        public long FieldCount => _length / PointerTools.Size<TPointer>();

        public long RowSize => _provider.ReadStruct<TPointer>(_start).LongValue;

        public DataProvider Provider => _provider;

        long IBlockEntity.Start => _start;

        long IBlockEntity.Length => _length + PointerTools.Size<TPointer>();

        public BlockOffset this[int index] => GetField(index);

        public BlockOffset this[long index] => GetField(index);

        public bool CheckValid()
        {
            if (_provider == null || _start < 0 || _length < 0)
                return false;

            TPointer ptr = PointerTools.FromOffset<TPointer>(1);
            long max = _provider.ReadStruct<TPointer>(_start).LongValue;
            long prev = -1, current;

            while (ptr.LongValue < _length)
            {
                current = _provider.ReadStruct<TPointer>(ptr.LongValue + _start).LongValue;
                if (current <= prev || current >= max)
                    return false;
                prev = current;
                ptr = ptr.Increment();
            }
            return true;
        }

        public BlockOffset GetField(long index)
        {
            long count = FieldCount;
            if (index >= count || index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));

            long start = 0, end = RowSize;
            if (index != 0)
                start = _provider.ReadStruct<TPointer>(_start + PointerTools.Size<TPointer>() * index).LongValue;

            if (++index != count)
                end = _provider.ReadStruct<TPointer>(_start + PointerTools.Size<TPointer>() * index).LongValue;

            return new BlockOffset(start, end - start);
        }
    }
}

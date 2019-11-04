using System;
using System.Collections.Generic;
using System.Text;

namespace MimiTools.Data.Entities
{
    public readonly struct DataIndex<TPointer> : IBlockEntity where TPointer : unmanaged, IPointer<TPointer>
    {
        private readonly DataProvider _provider;
        private readonly long _start;
        private readonly long _length;

        public DataIndex(in Block block)
        {
            _provider = block.Provider;

            /*
             * This class actually used to be part of IndexedBlock<TPointer>, but when it was discovered that IndexedBlock was basically a HeaderBlock with an index in it,
             * I decided to seperate it. However the code always assumed we start at the beginning of the header block, So I needed to adapt for that, and it was more efficient
             * to make it operate like before, except with an exception for index 0, so here we pretend we are the whole header block 
            */
            _start = block.Start - PointerTools.Size<TPointer>();
            _length = block.Length + PointerTools.Size<TPointer>();
        }

        public int Count => (int) (_length / PointerTools.Size<TPointer>());

        public BlockOffset this[int index] => GetIndex(index);

        DataProvider IBlockEntity.Provider => _provider;

        long IBlockEntity.Start => _start + PointerTools.Size<TPointer>();

        long IBlockEntity.Length => _length - PointerTools.Size<TPointer>();

        public bool CheckValid()
        {
            if (_provider == null || _start < 0 || _length < 0)
                return false;

            TPointer ptr = PointerTools.FromOffset<TPointer>(1);
            long prev = -1, current;

            while (ptr.LongValue < _length)
            {
                current = _provider.ReadStruct<TPointer>(ptr.LongValue + _start).LongValue;
                if (current <= prev)
                    return false;
                prev = current;
                ptr = ptr.Increment();
            }
            return true;
        }

        public BlockOffset GetIndex(int index)
        {
            int count = Count;
            if (index >= count)
                throw new IndexOutOfRangeException();

            long start;
            if (index == 0)
                start = 0;
            else
                start = _provider.ReadStruct<TPointer>(_start + (index * PointerTools.Size<TPointer>())).LongValue;

            if (++index == count)
                return new BlockOffset(start, -1);

            long end = _provider.ReadStruct<TPointer>(_start + (index * PointerTools.Size<TPointer>())).LongValue;

            return new BlockOffset(start, end - start);
        }
    }
}

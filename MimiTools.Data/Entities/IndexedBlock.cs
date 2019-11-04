using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace MimiTools.Data.Entities
{
    public readonly struct IndexedBlock<TPointer> : IBlockEntity where TPointer : unmanaged, IPointer<TPointer>
    {
        public IndexedBlock(in Block block) : this()
        {
            if (!block.CheckValid())
                throw new ArgumentException("Specified block isn't valid!");

            HeaderBlock<TPointer> h_block = block.AsHeader<TPointer>();

            if (!h_block.CheckValid())
                throw new ArgumentException("Specified block isn't a valid header block!");

            _index = new DataIndex<TPointer>(h_block.Header);

            if (!_index.CheckValid())
                throw new ArgumentException("Block header doesn't contain a valid index!");

            _provider = block.Provider;
            _start = h_block.Body.Start;
            _length = h_block.Body.Length;
        }

        public IndexedBlock(in Block block, in DataIndex<TPointer> index)
        {
            if (!block.CheckValid())
                throw new ArgumentException("Specified block isn't valid!");

            if (!index.CheckValid())
                throw new ArgumentException("Specified index isn't valid!");

            _provider = block.Provider;
            _index = index;
            _start = block.Start;
            _length = block.Length;
        }

        private readonly DataIndex<TPointer> _index;
        private readonly DataProvider _provider;
        private readonly long _start;
        private readonly long _length;

        public long Count => _index.Count;

        DataProvider IBlockEntity.Provider => _provider;

        long IBlockEntity.Start => _start;

        long IBlockEntity.Length => _length;

        public Block this[int index] => GetBlock(index);

        public bool CheckValid()
        {
            if (_provider == null || _start < 0 || _length < 0)
                return false;

            return _index.CheckValid();
        }

        public Block GetBlock(int index)
            => _index[index].Apply(this);
    }
}

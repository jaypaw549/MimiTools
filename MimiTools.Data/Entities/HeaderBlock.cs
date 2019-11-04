using System;
using System.Collections.Generic;
using System.Text;

namespace MimiTools.Data.Entities
{
    public readonly struct HeaderBlock<TPointer> : IBlockEntity where TPointer : unmanaged, IPointer<TPointer>
    {
        private readonly DataProvider _provider;
        private readonly long _start;
        private readonly long _length;

        public HeaderBlock(in Block block)
        {
            _provider = block.Provider;
            _start = block.Start;
            _length = block.Length;
        }

        public Block Body
        {
            get
            {
                long split = _provider.ReadStruct<TPointer>(_start).LongValue;
                return new Block(_provider, _start + split, _length - split);
            }
        }

        public Block Header => new Block(_provider, _start + PointerTools.Size<TPointer>(), _provider.ReadStruct<TPointer>(_start).LongValue - PointerTools.Size<TPointer>());

        DataProvider IBlockEntity.Provider => _provider;

        long IBlockEntity.Start => _start;

        long IBlockEntity.Length => _length;

        public bool CheckValid()
        {
            if (_provider == null || _start < 0 || _length < 0)
                return false;

            long split = _provider.ReadStruct<TPointer>(_start).LongValue;
            return split <= _length && split >= PointerTools.Size<TPointer>();
                
        }
    }
}

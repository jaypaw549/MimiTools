using MimiTools.Data.Builders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace MimiTools.Data.Builders
{
    public sealed class IndexedBlockBuilder<TPointer> : BlockBuilder, IIndexedBlockBuilder where TPointer : unmanaged, IPointer<TPointer>
    {
        private readonly List<BlockBuilder> _blocks = new List<BlockBuilder>();

        public BlockBuilder Data { get; }

        public BlockBuilder Index { get; }

        public override long Size => _blocks.Aggregate(PointerTools.Zero<TPointer>(), (a, b) => a.Add(b.Size).Increment()).LongValue;

        IBlockBuilder IIndexedBlockBuilder.Data => Data;

        IBlockBuilder IIndexedBlockBuilder.Index => Index;

        public IndexedBlockBuilder()
        {
            Data = new DataBuilder(this);
            Index = new IndexBuilder(this);
        }

        public IndexedBlockBuilder<TPointer> AddBlock(IBlockBuilder block)
        {
            _blocks.Add(block.ToBuilder());
            return this;
        }

        public IndexedBlockBuilder<TPointer> AddBlock(long size)
        {
            _blocks.Add(new ZeroBlockBuilder(size));
            return this;
        }

        private void WriteBody(Stream stream)
        {
            foreach (BlockBuilder builder in _blocks)
                builder.WriteTo(stream);
        }

        protected override void WriteData(Stream stream)
        {
            byte[] header = MemoryMarshal.AsBytes(stackalloc TPointer[] { PointerTools.FromOffset<TPointer>(_blocks.Count) }).ToArray();
            stream.Write(header, 0, header.Length);
            WriteIndex(stream);
            WriteBody(stream);
        }

        private void WriteIndex(Stream stream)
        {
            TPointer ptr = PointerTools.Zero<TPointer>();
            Span<TPointer> index = _blocks.Count * ptr.PtrSize > 1024 ? new TPointer[_blocks.Count-1] : stackalloc TPointer[_blocks.Count-1];

            for (int i = 0; i < _blocks.Count-1; i++)
            {
                ptr = ptr.Add(_blocks[i].Size);
                index[i] = ptr;
            }

            byte[] index_data = MemoryMarshal.AsBytes(index).ToArray();
            stream.Write(index_data, 0, index_data.Length);
        }

        IIndexedBlockBuilder IIndexedBlockBuilder.AddBlock(IBlockBuilder block)
            => AddBlock(block);

        IIndexedBlockBuilder IIndexedBlockBuilder.AddBlock(long size)
            => AddBlock(size);

        private sealed class DataBuilder : BlockBuilder
        {
            private readonly IndexedBlockBuilder<TPointer> _base;

            internal DataBuilder(IndexedBlockBuilder<TPointer> @base)
                => _base = @base;

            public override long Size => _base._blocks.Count * PointerTools.Size<TPointer>();

            protected override void WriteData(Stream stream)
                => _base.WriteBody(stream);
        }

        private sealed class IndexBuilder : BlockBuilder
        {
            private readonly IndexedBlockBuilder<TPointer> _base;

            internal IndexBuilder(IndexedBlockBuilder<TPointer> @base)
                => _base = @base;

            public override long Size => _base._blocks.Count * PointerTools.Size<TPointer>();

            protected override void WriteData(Stream stream)
                => _base.WriteIndex(stream);
        }
    }
}

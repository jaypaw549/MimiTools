using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MimiTools.Data.Builders
{
    internal sealed class CopyBlockBuilder : BlockBuilder
    {
        internal CopyBlockBuilder(Block block)
        {
            if (!block.CheckValid())
                throw new ArgumentException("Block isn't a valid block!");
            _block = block;
        }

        private readonly Block _block;

        public override long Size => _block.Length;

        protected override void WriteData(Stream stream)
        {
            byte[] data = _block.ReadAsArray<byte>();
            stream.Write(data, 0, data.Length);
        }
    }
}

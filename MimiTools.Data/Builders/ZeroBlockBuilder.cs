using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MimiTools.Data.Builders
{
    internal sealed class ZeroBlockBuilder : BlockBuilder
    {
        internal ZeroBlockBuilder(long size)
        {
            Size = size;
        }

        public override long Size { get; }

        protected override void WriteData(Stream stream)
            => stream.Write(new byte[(int)Size], 0, (int)Size);
    }
}

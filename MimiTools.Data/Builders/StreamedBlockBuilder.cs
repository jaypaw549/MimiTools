using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MimiTools.Data.Builders
{
    public sealed class StreamedBlockBuilder : BlockBuilder
    {
        public StreamedBlockBuilder(Stream stream)
        {
            Stream = stream;
        }
        
        public override long Size => Stream.Length;

        public Stream Stream { get; }

        protected override void WriteData(Stream stream)
        {
            Stream.CopyTo(stream);
        }
    }
}

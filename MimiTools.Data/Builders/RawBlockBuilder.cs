using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MimiTools.Data.Builders
{
    public sealed class RawBlockBuilder : BlockBuilder
    {
        public byte[] Data { get; }

        public override long Size => Data.LongLength;

        public RawBlockBuilder(byte[] data)
        {
            Data = data;
        }

        public RawBlockBuilder(int size)
        {
            Data = new byte[size];
        }

        protected override void WriteData(Stream stream)
            => stream.Write(Data, 0, Data.Length);
    }
}

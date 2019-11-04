using MimiTools.Data.Builders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace MimiTools.Data.Builders
{
    public abstract class BlockBuilder : IBlockBuilder
    {
        public abstract long Size { get; }

        public byte[] ToByteArray()
        {
            byte[] data = new byte[Size];
            using (MemoryStream stream = new MemoryStream(data))
                WriteData(stream);
            return data;
        }

        protected abstract void WriteData(Stream stream);

        public void WriteTo(Stream stream)
        {
            WriteData(new RestrictedAccessStream(stream));
            stream.Flush();
        }

        BlockBuilder IBlockBuilder.ToBuilder()
            => this;
    }
}

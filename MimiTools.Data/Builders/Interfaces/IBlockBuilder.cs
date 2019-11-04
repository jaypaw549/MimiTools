using System.IO;

namespace MimiTools.Data.Builders
{
    public interface IBlockBuilder
    {
        long Size { get; }

        BlockBuilder ToBuilder();

        byte[] ToByteArray();
        void WriteTo(Stream stream);
    }
}
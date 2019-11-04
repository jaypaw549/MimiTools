using MimiTools.Data.Builders;

namespace MimiTools.Data.Builders
{
    public interface IIndexedBlockBuilder : IBlockBuilder
    {
        IIndexedBlockBuilder AddBlock(IBlockBuilder block);

        IIndexedBlockBuilder AddBlock(long size);

        IBlockBuilder Data { get; }

        IBlockBuilder Index { get; }
    }
}
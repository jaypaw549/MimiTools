namespace MimiTools.Data.Builders
{
    public interface IRowBlockBuilder : IBlockBuilder
    {
        ITableBlockBuilder Table { get; }

        IRowBlockBuilder SetField(int index, IBlockBuilder block);
    }
}
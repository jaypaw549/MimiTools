namespace MimiTools.Data.Builders
{
    public interface ITableBlockBuilder : IBlockBuilder
    {
        IRowBlockBuilder CreateRow();

        IHeaderBlockBuilder WithLayoutHeader();
    }
}
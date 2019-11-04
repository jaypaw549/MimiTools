namespace MimiTools.Data.Builders
{
    public interface IHeaderBlockBuilder : IBlockBuilder
    {
        IHeaderBlockBuilder SetBody(IBlockBuilder body);

        IHeaderBlockBuilder SetHead(IBlockBuilder head);
    }
}
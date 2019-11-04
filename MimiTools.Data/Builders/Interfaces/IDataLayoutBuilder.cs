using MimiTools.Data.Builders;
using MimiTools.Data.Entities;

namespace MimiTools.Data.Builders
{
    public interface IDataLayoutBuilder : IBlockBuilder
    {
        IDataLayoutBuilder AddField(int size);

        IDataLayoutBuilder AddField(long size);

        DataLayout<TPointer> Export<TPointer>() where TPointer : unmanaged, IPointer<TPointer>;
    }
}
using System.IO;

namespace MimiTools.Data.Builders
{
    public sealed class RowBlockBuilder<TPointer> : BlockBuilder, IRowBlockBuilder where TPointer : unmanaged, IPointer<TPointer>
    {
        public TableBlockBuilder<TPointer> Table { get; }

        public override long Size => _data.LongLength;

        ITableBlockBuilder IRowBlockBuilder.Table => Table;

        private readonly byte[] _data;

        internal RowBlockBuilder(TableBlockBuilder<TPointer> table)
        {
            Table = table;
            _data = new byte[table.Layout.RowSize];
        }

        public RowBlockBuilder<TPointer> SetField(int index, IBlockBuilder block)
        {
            BlockOffset field = Table.Layout.GetField(index);
            using (MemoryStream stream = new MemoryStream(_data, (int)field.Start, (int)field.Length, true, false))
                block.WriteTo(stream);

            return this;
        }

        protected override void WriteData(Stream stream)
        {
            stream.Write(_data, 0, _data.Length);
        }

        IRowBlockBuilder IRowBlockBuilder.SetField(int index, IBlockBuilder block)
            => SetField(index, block);
    }
}
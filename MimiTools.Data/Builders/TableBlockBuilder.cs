using MimiTools.Data.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MimiTools.Data.Builders
{
    public sealed class TableBlockBuilder<TPointer> : BlockBuilder, ITableBlockBuilder where TPointer : unmanaged, IPointer<TPointer>
    {
        private readonly List<RowBlockBuilder<TPointer>> _rows = new List<RowBlockBuilder<TPointer>>();

        public TableBlockBuilder(DataLayout<TPointer> layout)
        {
            Layout = layout;
        }

        public DataLayout<TPointer> Layout { get; }

        public override long Size => _rows.Count * Layout.RowSize;

        public RowBlockBuilder<TPointer> CreateRow()
        {
            RowBlockBuilder<TPointer> row = new RowBlockBuilder<TPointer>(this);
            _rows.Add(row);
            return row;
        }

        public HeaderBlockBuilder<TPointer> WithLayoutHeader()
            => new HeaderBlockBuilder<TPointer>(Layout.ToBlockBuilder(), this);

        protected override void WriteData(Stream stream)
        {
            foreach (RowBlockBuilder<TPointer> row in _rows)
                row.WriteTo(stream);
        }

        IRowBlockBuilder ITableBlockBuilder.CreateRow()
            => CreateRow();

        IHeaderBlockBuilder ITableBlockBuilder.WithLayoutHeader()
            => WithLayoutHeader();
    }
}

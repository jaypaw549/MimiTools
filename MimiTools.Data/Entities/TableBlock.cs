using System;
using System.Collections.Generic;
using System.Text;

namespace MimiTools.Data.Entities
{
    public readonly struct TableBlock<TPointer> : IBlockEntity where TPointer : unmanaged, IPointer<TPointer>
    {
        DataProvider IBlockEntity.Provider => _fields.Provider;

        long IBlockEntity.Start => _start;

        long IBlockEntity.Length => _length;

        public long RowCount => _length / _fields.RowSize;
        public long ColCount => _fields.FieldCount;

        private readonly long _start;
        private readonly long _length;
        private readonly DataLayout<TPointer> _fields;
        private readonly DataProvider _provider;

        public TableBlock(in Block block, in DataLayout<TPointer> layout)
        {
            if (!layout.CheckValid())
                throw new ArgumentException("Specified layout isn't a valid layout!");

            _start = block.Start;
            _length = block.Length;
            _fields = layout;
            _provider = block.Provider;
        }

        public TableBlock(in Block block)
        {
            HeaderBlock<TPointer> h_block = new HeaderBlock<TPointer>(block);

            if (!h_block.CheckValid())
                throw new ArgumentException("Specified block doesn't have a header!");

            _fields = h_block.Header.AsDataLayout<TPointer>();
            if (!_fields.CheckValid())
                throw new ArgumentException("Block Header doesn't contain a valid layout!");

            _start = h_block.Body.Start;
            _length = h_block.Body.Length;

            _provider = block.Provider;
        }

        public bool CheckValid()
        {
            if (_provider == null || _start < 0 || _length < 0)
                return false;

            long row_size = _fields.RowSize;
            if (row_size <= 0)
                return false;
            return _fields.CheckValid() && _length % row_size == 0;
        }

        /// <summary>
        /// Gets a specific cell of the table, convenience method doing the work of applying the result of <see cref="GetRow(int)"/> and <see cref="GetColumn(int)"/> in one function. 
        /// </summary>
        /// <param name="row">the row of the cell to get</param>
        /// <param name="col">the column of the cell to get</param>
        /// <returns>A block that covers only that cell of the table</returns>
        /// <seealso cref="GetCell(int, int)"/>
        public Block GetCell(int row, int col)
            =>_fields[col].Apply(GetRow(row));

        /// <summary>
        /// Gets the offset for a specific table column, apply to a block obtained from <see cref="GetRow(int)"/> to get a specific cell of that row.
        /// </summary>
        /// <param name="col">the column index to get</param>
        /// <returns>a BlockOffset which can be applied to a block from <see cref="GetRow(int)">GetRow</see> to obtain a specific cell</returns>
        /// <seealso cref="GetRow(int)"/>
        /// <seealso cref="GetCell(int, int)"/>
        public BlockOffset GetColumn(int col)
            => _fields.GetField(col);

        /// <summary>
        /// Gets a block representing a row of the table, apply a <see cref="BlockOffset{TPointer}"/> to the result to obtain a specific cell.
        /// </summary>
        /// <param name="row">the row index number to get</param>
        /// <returns>A block representing an entire row of the table</returns>
        public Block GetRow(int row)
        {
            if (row >= RowCount || row < 0)
                throw new ArgumentOutOfRangeException(nameof(row));

            return new Block(_fields.Provider, (_fields.RowSize * row) + _start, _fields.RowSize);
        }
    }
}

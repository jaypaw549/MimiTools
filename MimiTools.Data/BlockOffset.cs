using System;
using System.Collections.Generic;
using System.Text;

namespace MimiTools.Data
{
    /// <summary>
    /// Block offset, can be used to get a subsection of another block.
    /// </summary>
    public readonly struct BlockOffset
    {
        /// <summary>
        /// The basic constructor
        /// </summary>
        /// <param name="start">The start of the block offset</param>
        /// <param name="length">The length of the block offset</param>
        public BlockOffset(long start, long length)
        {
            Start = start;
            Length = length;
        }

        public long Start { get; }

        public long Length { get; }

        internal Block Apply<TEntity>(TEntity entity) where TEntity : IBlockEntity
        {
            if (Start + Length > entity.Length)
                throw new ArgumentOutOfRangeException(nameof(entity));

            if (Length == -1)
                return new Block(entity.Provider, entity.Start + Start, entity.Length - Start);
            return new Block(entity.Provider, entity.Start + Start, Length);
        }
    }
}

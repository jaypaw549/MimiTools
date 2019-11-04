using MimiTools.Data.Builders;
using MimiTools.Data.Entities;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace MimiTools.Data
{
    public static class BlockExtensions
    {
        /// <summary>
        /// Interprets a block as a data index block, which is generally responsible for partitioning another block of data into sub-blocks
        /// </summary>
        /// <typeparam name="TEntity">The pointer type to use for defining sub-blocks</typeparam>
        /// <param name="block">The block containing the data index</param>
        /// <returns>A data index</returns>
        public static DataIndex<TPointer> AsDataIndex<TPointer>(this in Block block) where TPointer : unmanaged, IPointer<TPointer>
            => new DataIndex<TPointer>(block);

        /// <summary>
        /// Interprets a block as a data layout block, which is generally responsible for defining row data in tables
        /// </summary>
        /// <typeparam name="TPointer">The pointer type to use for defining fields</typeparam>
        /// <param name="block">The block containing the data layout</param>
        /// <returns>A data layout</returns>
        public static DataLayout<TPointer> AsDataLayout<TPointer>(this in Block block) where TPointer : unmanaged, IPointer<TPointer>
            => new DataLayout<TPointer>(block);

        /// <summary>
        /// Interprets a block as a block with a header, often layouts or indexes are stored in the header
        /// </summary>
        /// <typeparam name="TPointer">Pointer type to use for defining the header boundary</typeparam>
        /// <param name="block">A block containing a header</param>
        /// <returns>a block interpreted as a block with a header</returns>
        public static HeaderBlock<TPointer> AsHeader<TPointer>(this in Block block) where TPointer : unmanaged, IPointer<TPointer>
            => new HeaderBlock<TPointer>(block);

        /// <summary>
        /// Interprets a block as a header block with an index for the header and applies the index to the body of the block
        /// </summary>
        /// <typeparam name="TPointer">The pointer type used to define the boundaries of the blocks</typeparam>
        /// <param name="block">The block containing a header with the index</param>
        /// <returns>A block (without the header) interpreted as an indexed block</returns>
        public static IndexedBlock<TPointer> AsIndexed<TPointer>(this in Block block) where TPointer : unmanaged, IPointer<TPointer>
            => new IndexedBlock<TPointer>(block);

        /// <summary>
        /// Partitions a block into sub-blocks defined by the specified index.
        /// </summary>
        /// <typeparam name="TPointer">The pointer type used by the data index</typeparam>
        /// <param name="block">The block to partition</param>
        /// <param name="index">The index defining the block partitions</param>
        /// <returns>A block interpreted as an indexed block</returns>
        public static IndexedBlock<TPointer> AsIndexed<TPointer>(this in Block block, in DataIndex<TPointer> index) where TPointer : unmanaged, IPointer<TPointer>
            => new IndexedBlock<TPointer>(block, index);

        /// <summary>
        /// Interprets the block as a header block with a layout for the header and applies the layout to the body of the block.
        /// </summary>
        /// <typeparam name="TPointer">The pointer type used in the layout</typeparam>
        /// <param name="block">The block to index</param>
        /// <returns>a block interpreted as a table block</returns>
        public static TableBlock<TPointer> AsTable<TPointer>(this in Block block) where TPointer : unmanaged, IPointer<TPointer>
            => new TableBlock<TPointer>(block);

        /// <summary>
        /// Partitions a block into rows and fields using the specified data layout.
        /// </summary>
        /// <typeparam name="TPointer">The pointer type used by the layout</typeparam>
        /// <param name="block">The block to interpret as a data block</param>
        /// <param name="layout">The layout to interpret the block with</param>
        /// <returns>A block interpreted as a table block</returns>
        public static TableBlock<TPointer> AsTable<TPointer>(this in Block block, in DataLayout<TPointer> layout) where TPointer : unmanaged, IPointer<TPointer>
            => new TableBlock<TPointer>(block, layout);

        /// <summary>
        /// Creates a sub-block of the specified block
        /// </summary>
        /// <param name="block">The block to create a sub-block from</param>
        /// <param name="offset">The offset defining the sub-block</param>
        /// <returns>A block that's a sub-block of the specified block</returns>
        public static Block CreateSubBlock(this in Block block, in BlockOffset offset)
            => offset.Apply(block);

        /// <summary>
        /// Convenience/compatibility method, reads a block into an array
        /// </summary>
        /// <param name="block">The block to read from</param>
        /// <param name="array">The array to read into</param>
        /// <param name="start">The position to start reading into the array at</param>
        /// <param name="length">How much data to read into the array</param>
        public static void Read(this in Block block, byte[] array, int start, int length)
            => block.Read(new Span<byte>(array, start, length));

        /// <summary>
        /// Reads the block as a string using the default encoding
        /// </summary>
        /// <param name="block">The block to read as a string</param>
        /// <returns>the string the block contained</returns>
        public static string ReadAsString(this in Block block)
            => Encoding.Default.GetString(block.ReadAsArray<byte>());

        /// <summary>
        /// Reads the block as a string using the specified encoding
        /// </summary>
        /// <param name="block">The block to read as a string</param>
        /// <param name="encoding">The encoding to read the string wtih</param>
        /// <returns>a string the block contained</returns>
        public static string ReadAsString(this in Block block, Encoding encoding)
            => encoding.GetString(block.ReadAsArray<byte>());

        /// <summary>
        /// Converts a block entity into a block builder, for use in building a new set of data.
        /// </summary>
        /// <typeparam name="TEntity">The type of block entity we're converting</typeparam>
        /// <param name="entity">the entity to convert</param>
        /// <returns>a new block builder which will copy the entity when it's built</returns>
        public static IBlockBuilder ToBlockBuilder<TEntity>(this TEntity entity) where TEntity : IBlockEntity
            => new CopyBlockBuilder(new Block(entity.Provider, entity.Start, entity.Length));
    }
}

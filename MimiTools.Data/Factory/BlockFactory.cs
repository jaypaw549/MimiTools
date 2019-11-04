using MimiTools.Data.Builders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace MimiTools.Data.Factory
{
    /// <summary>
    /// Block Factory class designed to make assembling your data a bit easier.
    /// </summary>
    public abstract class BlockFactory
    {
        protected BlockFactory() { }    

        /// <summary>
        /// Adds a header to another block builder
        /// </summary>
        /// <param name="header">The header for the other block</param>
        /// <param name="block">The block being given a header</param>
        /// <returns>A header block</returns>
        public IHeaderBlockBuilder AddHeader(IBlockBuilder header, IBlockBuilder block)
            => NewHeaderBlock().SetHead(header).SetBody(block);

        /// <summary>
        /// Creates a block builder from raw bytes
        /// </summary>
        /// <param name="data">the data the block will contain</param>
        /// <returns>A block builder containing the raw data provided</returns>
        public RawBlockBuilder FromBytes(byte[] data)
            => BuildFromBytes(data);

        /// <summary>
        /// Creates a block from a string using the default encoding
        /// </summary>
        /// <param name="data">The string the block will contain</param>
        /// <returns>A block builder containing the string</returns>
        public IBlockBuilder FromString(string data)
            => FromString(data, Encoding.Default);

        /// <summary>
        /// Creates a block from a string using the specified encoding
        /// </summary>
        /// <param name="data">The string the block will contain</param>
        /// <param name="encoding">The encoding to write the string with</param>
        /// <returns>A block builder containing the string</returns>
        public virtual IBlockBuilder FromString(string data, Encoding encoding)
            => BuildFromBytes(encoding.GetBytes(data));

        /// <summary>
        /// Creates a block from a span of structs (which could be bytes, ints, or a custom type defined by you)
        /// </summary>
        /// <typeparam name="TStruct">The type of data you're making a block from</typeparam>
        /// <param name="span">The data the block will contain</param>
        /// <returns>a block builder containing the data of the span</returns>
        public virtual IBlockBuilder FromSpan<TStruct>(Span<TStruct> span) where TStruct : unmanaged
            => BuildFromBytes(MemoryMarshal.AsBytes(span).ToArray());

        /// <summary>
        /// Creates a block from the specified struct (which could be a byte, int, or a custom type defined by you)
        /// </summary>
        /// <typeparam name="TStruct">The type of data you want to build a block from</typeparam>
        /// <param name="value">The value the block will contain</param>
        /// <returns>A block builder containing the data of the struct</returns>
        public virtual IBlockBuilder FromValue<TStruct>(in TStruct value) where TStruct : unmanaged
            => BuildFromBytes(MemoryMarshal.AsBytes(stackalloc TStruct[] { value }).ToArray());

        /// <summary>
        /// Creates a block from an array of structs (which could be bytes, ints, or a custom type defined by you)
        /// </summary>
        /// <typeparam name="TStruct">The type of data you're making a block from</typeparam>
        /// <param name="array">The data the block will contain</param>
        /// <returns>a block builder containing the data of the span</returns>
        public virtual IBlockBuilder FromValueArray<TStruct>(TStruct[] array) where TStruct : unmanaged
            => BuildFromBytes(MemoryMarshal.AsBytes<TStruct>(array).ToArray());

        /// <summary>
        /// Creates a builder for a data layout, these are commonly used to define table rows and are necessary for tables to function.
        /// </summary>
        /// <returns>A data layout builder</returns>
        public abstract IDataLayoutBuilder NewDataLayout();

        /// <summary>
        /// Creates a builder for an indexed block, the builder provided can have data and index seperated.
        /// </summary>
        /// <returns>An indexed block builder</returns>
        public abstract IIndexedBlockBuilder NewIndexedBlock();

        /// <summary>
        /// Creates a builder for a header block, which contains a head and a body. Head is commonly used to store data about the body,
        /// such as a data layout for a table, or a data index for an indexed block.
        /// </summary>
        /// <returns>A header block builder</returns>
        public abstract IHeaderBlockBuilder NewHeaderBlock();

        /// <summary>
        /// Creates a new builder for a table block, the specified layout is built and used for defining the table rows.
        /// </summary>
        /// <param name="layout">The layout the table will use</param>
        /// <returns>A table block builder, which can be used to add rows to the table</returns>
        public abstract ITableBlockBuilder NewTableBlock(IDataLayoutBuilder layout);

        /// <summary>
        /// Creates a block builder from raw bytes
        /// </summary>
        /// <param name="data">the data the block will contain</param>
        /// <returns>A block builder containing the raw data provided</returns>
        public static RawBlockBuilder BuildFromBytes(byte[] data)
            => new RawBlockBuilder(data);

        /// <summary>
        /// Creates a block builder from a stream
        /// </summary>
        /// <param name="stream">the data the block will contain</param>
        /// <returns>A block builder that will read from the specified stream when it's building</returns>
        public static StreamedBlockBuilder FromStream(Stream stream)
            => new StreamedBlockBuilder(stream);

        /// <summary>
        /// Gets a factory for a specified pointer type.
        /// </summary>
        /// <typeparam name="TPointer">The pointer type blocks generated from the factory will use (if applicable)</typeparam>
        /// <returns>The factory for the specified pointer type</returns>
        public static BlockFactory GetFactory<TPointer>() where TPointer : unmanaged, IPointer<TPointer>
            => BlockFactory<TPointer>.Instance;
    }

    internal sealed class BlockFactory<TPointer> : BlockFactory where TPointer : unmanaged, IPointer<TPointer>
    {
        internal static BlockFactory<TPointer> Instance { get; } = new BlockFactory<TPointer>();
        private BlockFactory()
        {
        }

        public override IDataLayoutBuilder NewDataLayout()
            => new DataLayoutBuilder<TPointer>();

        public override IHeaderBlockBuilder NewHeaderBlock()
            => new HeaderBlockBuilder<TPointer>();

        public override IIndexedBlockBuilder NewIndexedBlock()
            => new IndexedBlockBuilder<TPointer>();

        public override ITableBlockBuilder NewTableBlock(IDataLayoutBuilder layout)
            => new TableBlockBuilder<TPointer>(layout.Export<TPointer>());
    }
}

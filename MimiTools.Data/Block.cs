using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace MimiTools.Data
{
    /// <summary>
    /// The most fundamental of entities, the block. It provides basic reading capabilities for raw data.
    /// </summary>
    public readonly struct Block : IBlockEntity
    {
        /// <summary>
        /// The provider that the block gets its data from.
        /// </summary>
        public DataProvider Provider { get; }

        /// <summary>
        /// The point from the provider it will start to read data from
        /// </summary>
        public long Start { get; }

        /// <summary>
        /// The amount of data the block will read from the provider
        /// </summary>
        public long Length { get; }

        internal Block(DataProvider source, long start, long length)
        {
            Provider = source;
            Start = start;
            Length = length;
        }

        /// <summary>
        /// Checks the basic requirements: does it have a provider, and will it read from a valid spot and length.
        /// </summary>
        /// <returns></returns>
        public bool CheckValid()
            => Provider != null && Start >= 0 && Length >= 0;

        /// <summary>
        /// Reads data into the specified span.
        /// </summary>
        /// <param name="data">The place to store the data in</param>
        public void Read(Span<byte> data)
        {
            if (data.Length > Length)
                throw new IndexOutOfRangeException();

            Provider.Read(Start, data);
        }

        /// <summary>
        /// Reads the block as the specified struct
        /// </summary>
        /// <typeparam name="TStruct">The type to interpret the data as</typeparam>
        /// <returns>A struct built from the data in the block</returns>
        public TStruct ReadAs<TStruct>() where TStruct : unmanaged
            => Provider.ReadStruct<TStruct>(Start);

        /// <summary>
        /// Reads the block as an array of structs, which can be primitive or user-defined
        /// </summary>
        /// <typeparam name="TStruct">The type to interpret the data as</typeparam>
        /// <returns></returns>
        public TStruct[] ReadAsArray<TStruct>() where TStruct : unmanaged
        {
            Span<byte> data = Length > 1024 ? new byte[Length] : stackalloc byte[(int) Length];
            Provider.Read(Start, data);
            return MemoryMarshal.Cast<byte, TStruct>(data).ToArray();
        }
    }
}

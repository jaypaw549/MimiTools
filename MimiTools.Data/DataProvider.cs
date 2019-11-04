using MimiTools.Data.Providers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace MimiTools.Data
{
    /// <summary>
    /// The Data provider class which is the backend for all valid blocks and block entities
    /// </summary>
    public abstract class DataProvider
    {
        /// <summary>
        /// The number of bytes this provider's source has
        /// </summary>
        public abstract long Length { get; }

        /// <summary>
        /// Creates a block that covers the entire provider
        /// </summary>
        /// <returns>a block covering the entire data the provider has</returns>
        public Block AsBlock()
            => new Block(this, 0, Length);

        /// <summary>
        /// Reads bytes from the data source
        /// </summary>
        /// <param name="location">The position to read from</param>
        /// <param name="dest">The location to write the data to</param>
        /// <returns>The amount of bytes read</returns>
        public abstract int Read(long location, Span<byte> dest);
        
        /// <summary>
        /// Reads a struct from the data source
        /// </summary>
        /// <typeparam name="TStruct"></typeparam>
        /// <param name="location"></param>
        /// <returns></returns>
        public virtual TStruct ReadStruct<TStruct>(long location) where TStruct : unmanaged
        {
            Span<TStruct> data = stackalloc TStruct[1];

            //A quick hack that makes the provider write the struct directly
            int read = Read(location, MemoryMarshal.AsBytes(data));
            if (data.Length > read)
                throw new IOException("Was unable to read enough data!");
            return data[0];
        }

        /// <summary>
        /// Creates a Data provider from an array of bytes. This is the second fastest option available
        /// </summary>
        /// <param name="array">The data to create a provider for</param>
        /// <returns>A provider which allows parsing of the data in the array</returns>
        public static ArrayProvider FromArray(byte[] array)
            => new ArrayProvider(array);

        /// <summary>
        /// Creates a data provider from a stream, This is the slowest option available
        /// </summary>
        /// <param name="stream">The stream to read data from, must be seekable</param>
        /// <returns>A provider which allows parsing of the data in the stream</returns>
        public static StreamProvider FromStream(Stream stream, bool dispose = true)
            => new StreamProvider(stream, dispose);

        /// <summary>
        /// Creates a data provider from unmanaged or pinned memory
        /// This is just slightly faster than a StreamProvider
        /// </summary>
        /// <param name="ptr">the start of the unmanaged memory block</param>
        /// <param name="length">the length of the unmanaged memory block</param>
        /// <returns>an unmanaged data provider</returns>
        public static UnmanagedProvider FromUnmanaged(IntPtr ptr, long length)
            => new UnmanagedProvider(ptr, length);

        /// <summary>
        /// Creates a data provider from unmanaged or pinned memory, using raw pointers to parse it.
        /// This is the fastest provider, but requires the ability to use unsafe/unverifiable code.
        /// </summary>
        /// <param name="ptr">the start of the unmanaged memory block</param>
        /// <param name="length">the length of the unmanaged memory block</param>
        /// <returns>an unsafe data provider</returns>
        public static UnsafeProvider FromUnsafe(IntPtr ptr, long length)
            => new UnsafeProvider(ptr, length);
    }
}

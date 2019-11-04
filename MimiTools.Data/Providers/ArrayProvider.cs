using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace MimiTools.Data.Providers
{
    public class ArrayProvider : DataProvider
    {
        private readonly byte[] _data;

        /// <summary>
        /// Creates a data provider from the specified array
        /// </summary>
        /// <param name="data">The data the provider will read from</param>
        public ArrayProvider(byte[] data)
        {
            _data = data;
        }
        
        /// <summary>
        /// The length of the wrapped array
        /// </summary>
        public override long Length => _data.LongLength;

        /// <summary>
        /// Reads data from the the array at the specified location
        /// </summary>
        /// <param name="location">The location in the array to read from</param>
        /// <param name="dest">The location to copy the bytes to</param>
        /// <returns>the amount of bytes read</returns>
        public override int Read(long location, Span<byte> dest)
        {
            if (dest.Length > _data.LongLength - location)
                dest = dest.Slice(0, (int) (_data.LongLength - location));

            if (location < int.MaxValue)
                new Span<byte>(_data, (int)location, dest.Length).CopyTo(dest);
            else
                for (int i = 0; i < dest.Length; i++)
                    dest[i] = _data[i + location];

            return dest.Length;
        }
    }
}

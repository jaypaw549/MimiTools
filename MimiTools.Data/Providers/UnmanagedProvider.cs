using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace MimiTools.Data.Providers
{
    public sealed class UnmanagedProvider : DataProvider
    {
        private readonly IntPtr _start;
        private readonly long _length;

        public UnmanagedProvider(IntPtr start, long length)
        {
            _start = start;
            _length = length;
        }
        public override long Length => _length;

        public override int Read(long location, Span<byte> dest)
        {
            IntPtr ptr = new IntPtr(_start.ToInt64() + location);

            if (location + dest.Length > _length)
                dest = dest.Slice(0, (int) (_length - location));

            for (int i = 0; i < dest.Length; i++)
                dest[i] = Marshal.ReadByte(IntPtr.Add(ptr, i));

            return dest.Length;
        }
    }
}

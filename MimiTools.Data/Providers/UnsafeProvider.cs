using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace MimiTools.Data.Providers
{
    public unsafe sealed class UnsafeProvider : DataProvider
    {
        private readonly byte* _ptr;
        private readonly long _length;

        public UnsafeProvider(IntPtr ptr, long length)
        {
            _ptr = (byte*)ptr;
            _length = length;
        }

        public UnsafeProvider(void* ptr, long length)
        {
            _ptr = (byte*)ptr;
            _length = length;
        }

        public override long Length => _length;

        public override int Read(long location, Span<byte> dest)
        {
            byte* s = _ptr + location;
            if (dest.Length + location > _length)
                dest = dest.Slice(0, (int)(_length - location));

            new Span<byte>(s, dest.Length).CopyTo(dest);
            
            return dest.Length;
        }

        public override TStruct ReadStruct<TStruct>(long location)
        {
            if (sizeof(TStruct) + location > _length)
                throw new IOException("Read would go out of bounds!");
            
            return *(TStruct*)(_ptr + location);
        }
    }
}

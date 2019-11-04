using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace MimiTools.Data.Providers
{
    public sealed class StreamProvider : DataProvider, IDisposable
    {
        private readonly Stream _stream;
        private readonly bool _dispose;
        public StreamProvider(Stream stream, bool dispose = true)
        {
            if (!stream.CanSeek)
                throw new NotSupportedException("Stream must be able to seek!");

            _dispose = dispose;
            _stream = stream;
        }

        public override long Length => _stream.Length;

        public override int Read(long location, Span<byte> dest)
        {
            _stream.Seek(location, SeekOrigin.Begin);

            for (int i = 0; i < dest.Length; i++)
            {
                int value = _stream.ReadByte();
                if (value == -1)
                    return i;
                dest[i] = (byte)value;
            }

            return dest.Length;
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing && _dispose)
                {
                    _stream.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~StreamProvider()
        // {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}

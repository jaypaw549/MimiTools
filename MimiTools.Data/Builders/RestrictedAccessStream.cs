using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MimiTools.Data.Builders
{
    internal sealed class RestrictedAccessStream : Stream
    {
        private readonly Stream _stream;
        private long _offset;

        internal RestrictedAccessStream(Stream target)
        {
            //Prevent nesting, should have same result regardless
            if (target is RestrictedAccessStream ras)
                target = ras._stream;

            _stream = target;
            if (target.CanSeek)
                _offset = target.Position;
        }

        public override bool CanRead => _stream.CanRead;

        public override bool CanSeek => _stream.CanSeek;

        public override bool CanWrite => _stream.CanWrite;

        public override long Length => _stream.Length - _offset;

        public override long Position
        { 
            get => _stream.Position - _offset;
            set
            {
                if (!_stream.CanSeek)
                    throw new NotSupportedException();

                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value));

                _stream.Position = value + _offset;
            }
        }

        public override bool CanTimeout => _stream.CanTimeout;

        public override int ReadTimeout { get => _stream.ReadTimeout; set => _stream.ReadTimeout = value; }

        public override int WriteTimeout { get => _stream.WriteTimeout; set => _stream.WriteTimeout = value; }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
            => _stream.BeginRead(buffer, offset, count, callback, state);

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
            => _stream.BeginWrite(buffer, offset, count, callback, state);

        public override void Close() { }

        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
            => _stream.CopyToAsync(destination, bufferSize, cancellationToken);

        public override int EndRead(IAsyncResult asyncResult)
            => _stream.EndRead(asyncResult);

        public override void EndWrite(IAsyncResult asyncResult)
            => _stream.EndWrite(asyncResult);

        public override void Flush() { }

        public override Task FlushAsync(CancellationToken cancellationToken)
            => Task.CompletedTask;

        public override int Read(byte[] buffer, int offset, int count)
            => _stream.Read(buffer, offset, count);

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => _stream.ReadAsync(buffer, offset, count, cancellationToken);

        public override int ReadByte()
            => _stream.ReadByte();

        public override long Seek(long offset, SeekOrigin origin)
        {
            offset += _offset;
            switch (origin)
            {
                case SeekOrigin.Begin:
                    offset += _offset;
                    break;
                case SeekOrigin.Current:
                    if (offset + _stream.Position < _offset)
                        throw new ArgumentOutOfRangeException(nameof(offset));
                    break;
                case SeekOrigin.End:
                    if (offset + _stream.Length < _offset)
                        throw new ArgumentOutOfRangeException(nameof(offset));
                    break;
            }
            return _stream.Seek(offset, origin) - _offset;
        }

        public override void SetLength(long value)
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(nameof(value));

            _stream.SetLength(value + _offset);
        }

        public override void Write(byte[] buffer, int offset, int count)
            => _stream.Write(buffer, offset, count);

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => _stream.WriteAsync(buffer, offset, count, cancellationToken);

        public override void WriteByte(byte value)
            => _stream.WriteByte(value);
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace MimiTools.Data.Builders
{
    public sealed class HeaderBlockBuilder<TPointer> : BlockBuilder, IHeaderBlockBuilder where TPointer : unmanaged, IPointer<TPointer>
    {
        private BlockBuilder _head;
        private BlockBuilder _body;

        public HeaderBlockBuilder()
        {
            _head = null;
            _body = null;
        }

        public HeaderBlockBuilder(IBlockBuilder head)
        {
            _head = head.ToBuilder();
            _body = null;
        }

        public HeaderBlockBuilder(IBlockBuilder head, IBlockBuilder body)
        {
            _head = head.ToBuilder();
            _body = body.ToBuilder();
        }

        public override long Size => (_head?.Size ?? 0) + (_body?.Size ?? 0);

        public HeaderBlockBuilder<TPointer> SetBody(IBlockBuilder body)
        {
            _body = body.ToBuilder();
            return this;
        }

        public HeaderBlockBuilder<TPointer> SetHead(IBlockBuilder head)
        {
            _head = head.ToBuilder();
            return this;
        }

        protected override void WriteData(Stream stream)
        {
            Span<byte> ptr = MemoryMarshal.AsBytes(stackalloc TPointer[] { PointerTools.FromAddr<TPointer>(_head?.Size ?? 0).Increment() });
            stream.Write(ptr.ToArray(), 0, ptr.Length);
            _head?.WriteTo(stream);
            _body?.WriteTo(stream);
        }

        IHeaderBlockBuilder IHeaderBlockBuilder.SetBody(IBlockBuilder body)
            => SetBody(body);

        IHeaderBlockBuilder IHeaderBlockBuilder.SetHead(IBlockBuilder head)
            => SetHead(head);
    }
}

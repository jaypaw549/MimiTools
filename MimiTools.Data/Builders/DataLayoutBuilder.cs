using MimiTools.Data.Entities;
using MimiTools.Data.Builders;
using MimiTools.Data.Providers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace MimiTools.Data.Builders
{
    public sealed class DataLayoutBuilder<TPointer> : BlockBuilder, IDataLayoutBuilder where TPointer : unmanaged, IPointer<TPointer>
    {
        private readonly List<long> _fields = new List<long>();

        public override long Size => PointerTools.Size<TPointer>() * _fields.Count;

        public DataLayoutBuilder<TPointer> AddField(int size)
        {
            _fields.Add(size);
            return this;
        }

        public DataLayoutBuilder<TPointer> AddField(long size)
        {
            _fields.Add(size);
            return this;
        }

        public DataLayout<TPointer> Export()
            => new ArrayProvider(GetBytes()).AsBlock().AsDataLayout<TPointer>();

        private byte[] GetBytes()
        {
            TPointer ptr = PointerTools.FromAddr<TPointer>(_fields[0]);
            Span<TPointer> index = _fields.Count * ptr.PtrSize > 1024 ? new TPointer[_fields.Count] : stackalloc TPointer[_fields.Count];
            for (int i = 1; i < _fields.Count; i++)
            {
                index[i] = ptr;
                ptr = ptr.Add(_fields[i]);
            }
            if (index.Length > 0)
                index[0] = ptr;

            return MemoryMarshal.AsBytes(index).ToArray();
        }

        protected override void WriteData(Stream stream)
        {
            byte[] data = GetBytes();
            stream.Write(data, 0, data.Length);
        }

        IDataLayoutBuilder IDataLayoutBuilder.AddField(int size)
            => AddField(size);

        IDataLayoutBuilder IDataLayoutBuilder.AddField(long size)
            => AddField(size);

        DataLayout<XPointer> IDataLayoutBuilder.Export<XPointer>()
        {
            if (this is DataLayoutBuilder<XPointer> dlb)
                return dlb.Export();
            throw new InvalidCastException();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace MimiTools.Loader
{
    internal struct ByteData
    {
        public ByteData(byte[] data)
        {
            Hash = ByteArrayComparer.GetHashCode(data);
            Key = data;
        }

        private readonly byte[] Key;
        private readonly int Hash;

        public override bool Equals(object obj)
            => obj is ByteData data &&
                   Hash == data.Hash &&
                   ByteArrayComparer.Equals(Key, data.Key);

        public override int GetHashCode()
            => Hash;
    }
}

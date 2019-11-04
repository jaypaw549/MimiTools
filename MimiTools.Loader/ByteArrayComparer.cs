using System;
using System.Collections.Generic;
using System.Text;

namespace MimiTools.Loader
{
    internal class ByteArrayComparer : IEqualityComparer<byte[]>
    {
        public static ByteArrayComparer Instance = new ByteArrayComparer();

        private ByteArrayComparer() { }

        bool IEqualityComparer<byte[]>.Equals(byte[] x, byte[] y)
            => Equals(x, y);

        int IEqualityComparer<byte[]>.GetHashCode(byte[] data)
            => GetHashCode(data);

        public static bool Equals(byte[] x, byte[] y)
        {
            if (x == null)
                return y == null;

            if (y == null)
                return false;

            if (x.Length != y.Length)
                return false;

            for (int i = 0; i < x.Length; i++)
                if (x[i] != y[i])
                    return false;

            return true;
        }

        public static int GetHashCode(byte[] data)
        {
            if (data == null)
                return 0;

            const uint p = 16777619;
            uint hash = 2166136261;
            foreach (byte b in data)
                hash = (hash ^ b) * p;
            hash += hash << 13;
            hash ^= hash >> 7;
            hash += hash << 3;
            hash ^= hash >> 17;
            hash += hash << 5;
            return (int) hash;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Reflection;

namespace MimiTools.Loader
{
    internal class AsmNameComparer : IEqualityComparer<AssemblyName>
    {
        public static readonly AsmNameComparer Instance = new AsmNameComparer();

        bool IEqualityComparer<AssemblyName>.Equals(AssemblyName x, AssemblyName y)
            => Equals(x, y);

        int IEqualityComparer<AssemblyName>.GetHashCode(AssemblyName obj)
            => GetHashCode(obj);

        public static bool Equals(AssemblyName x, AssemblyName y)
            => x.Name.Equals(y.Name) && x.Version.Equals(y.Version) && ConvertToken(x.GetPublicKeyToken()) == ConvertToken(y.GetPublicKeyToken());

        public static int GetHashCode(AssemblyName obj)
        {
            int hash = 363513814 + obj.Name.GetHashCode();
            hash = hash * 363513814 + obj.Version.GetHashCode();
            return hash * 363513814 + ConvertToken(obj.GetPublicKeyToken()).GetHashCode();
        }

        public static long ConvertToken(byte[] v)
        {
            if (v != null && v.Length >= 8)
                return BitConverter.ToInt64(v, v.Length - 8);
            return 0;
        }
    }
}

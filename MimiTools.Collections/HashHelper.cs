using System;
using System.Threading;

namespace MimiTools.Collections
{
    internal static class HashHelper
    {
        private static int[] _primes = new int[0];

        //max cost of 14, instead of a max cost of 10000 using traditional array scanning. This is an ordered array after all.
        public static int NextPrime(int i)
        {
        start: int[] data = Volatile.Read(ref _primes);
            int lo = 0, hi = data.Length;

            while (lo < hi)
            {
                int mid, val = _primes[mid = (hi + lo) / 2];
                if (i < val)
                    hi = mid;

                //This will likely never run
                else if (i == val)
                    return val;
                else
                    lo = mid + 1;
            }

            if (hi == data.Length)
            {
                GenerateMorePrimes(data);
                goto start;
            }

            return _primes[hi];
        }

        private static unsafe void GenerateMorePrimes(int[] data)
        {
            int count = data.Length;
            Array.Resize(ref data, data.Length + 10000);

            int c;
            if (count > 0)
                c = data[count - 1];
            else
                c = 1;

            retry: while (count < data.Length)
            {
                c++;
                for (int i = 0; i < count; i++)
                    if (c % data[i] == 0)
                        goto retry;

                data[count++] = c;
            }
            Volatile.Write(ref _primes, data);
        }
    }
}

using System.Collections.Generic;

namespace MimiTools.Extensions.IComparer
{
    public static class IComparerExtensions
    {
        public static IComparer<T> Invert<T>(this IComparer<T> comparer)
            => Comparer<T>.Create((x, y) => comparer.Compare(y, x));
    }
}

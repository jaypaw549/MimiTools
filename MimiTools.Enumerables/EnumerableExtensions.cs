using System;
using System.Collections.Generic;
using System.Linq;

namespace MimiTools.Enumerables
{
    public static class EnumerableExtensions
    {
        public static IEnumerable<IEnumerable<T>> GroupNearby<T>(this IEnumerable<T> e, Func<T, bool> selector, int distance)
            => new NearbyGroupingEnumerable<T>(e, selector, distance);

        public static IOrderedEnumerable<T> SkipWhile<T>(this IOrderedEnumerable<T> e, Func<T, bool> predicate)
            => new SkipWhileOrderedEnumerable<T>(e, predicate);

        public static IOrderedEnumerable<T> Merge<T>(this IOrderedEnumerable<T> first, IOrderedEnumerable<T> second)
            => new MergedOrderedEnumerable<T>(first, second);

        public static IOrderedEnumerable<T> Merge<T>(this IOrderedEnumerable<T> first, IOrderedEnumerable<T> second, IComparer<T> comparer)
            => new MergedOrderedEnumerable<T>(first, second, comparer);

        public static IEnumerable<T> TakeNearby<T>(this IEnumerable<T> e, Func<T, bool> selector, int distance)
            => new NearbyOnlyEnumerable<T>(e, selector, distance);
    }
}

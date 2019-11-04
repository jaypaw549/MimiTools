using MimiTools.Tools;
using System;

namespace MimiTools.Extensions.Casting
{
    public static class CastExtensions
    {
        private static V Unbox<T, V>(this T value) where V : T
            => (V)value;

        public static V Cast<T, V>(this T value)
            => CastingTools.CreateCastingDelegate<T, V>().Unbox<Delegate, Func<T, V>>().Invoke(value);
    }
}

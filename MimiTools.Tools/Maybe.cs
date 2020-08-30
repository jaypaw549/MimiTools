using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace MimiTools.Tools
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Maybe<T>
    {
        private object alt;
        private T value;

        public static Maybe<T> Default => new Maybe<T>(default);

        public readonly bool IsMaybe => alt == null || alt is T;

        public readonly T MaybeValue => alt == null ? value : (T)alt;

        public readonly object MaybeNotValue => alt ?? value;

        public Maybe(T value)
        {
            this.value = value;
            alt = null;
        }

        public Maybe(object value)
        {
            this.value = default;
            alt = value;
        }

        public readonly V Value<V>()
        {
            if (alt != null && alt is V v_alt)
                return v_alt;

            if (value is V v_value)
                return v_value;

            return default;
        }

        public static implicit operator Maybe<T>(T value)
            => new Maybe<T>(value);

        internal static ref object GetAlternateField(ref Maybe<T> maybe)
            => ref maybe.alt;

        internal static ref T GetValueField(ref Maybe<T> maybe)
            => ref maybe.value;

        internal static ref readonly object GetReadonlyAlternateField(in Maybe<T> maybe)
            => ref maybe.alt;

        internal static ref readonly T GetReadonlyValueField(in Maybe<T> maybe)
            => ref maybe.value;
    }

    public static class Maybe
    {
        public static ref object GetAlternateField<T>(this ref Maybe<T> maybe)
            => ref Maybe<T>.GetAlternateField(ref maybe);

        public static ref T GetValueField<T>(this ref Maybe<T> maybe)
            => ref Maybe<T>.GetValueField(ref maybe);

        public static ref readonly object GetReadonlyAlternateField<T>(this in Maybe<T> maybe)
            => ref Maybe<T>.GetReadonlyAlternateField(maybe);

        public static ref readonly T GetReadonlyValueField<T>(this in Maybe<T> maybe)
            => ref Maybe<T>.GetReadonlyValueField(maybe);
    }
}

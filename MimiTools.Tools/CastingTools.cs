using System;

namespace MimiTools.Tools
{
    public static class CastingTools
    {
        public static Delegate CreateCastingDelegate(Type t, Type v)
        {
            Func<object, object> cast;

            if (t.IsAssignableFrom(v))
                cast = Unbox<object, object>;
            else if (v.IsAssignableFrom(t))
                cast = Box<object, object>;
            else
                throw new InvalidOperationException("The two types are unrelated!");

            return cast.Method.GetGenericMethodDefinition().MakeGenericMethod(t, v)
                .CreateDelegate(typeof(Func<,>).MakeGenericType(t, v));
        }

        private static V Box<T, V>(T value) where T : V
            => value;

        private static V Unbox<T, V>(T value) where V : T
            => (V)value;

        public static Func<T, V> CreateCastingDelegate<T, V>()
            => CreateCastingDelegate(typeof(T), typeof(V)) as Func<T, V>;
    }
}

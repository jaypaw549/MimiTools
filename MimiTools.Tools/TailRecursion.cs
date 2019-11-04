using System;
using System.Collections.Generic;
using System.Text;

namespace MimiTools.Tools
{
    public static class TailRecursion
    {
        public static TOut Execute<TOut>(this Func<Result<TOut>> func)
        {
            Result<TOut> result;
            do
                result = func();
            while (result._func != null);

            return result._value;
        }

        public static Result<TOut> Next<TOut>(Func<Result<TOut>> next)
            => new Result<TOut>(next);

        public static Result<TOut> Return<TOut>(TOut value)
            => new Result<TOut>(value);

        public readonly struct Result<TOut>
        {
            internal Result(TOut value)
            {
                _func = null;
                _value = value;
            }

            internal Result(Func<Result<TOut>> next)
            {
                _func = next;
                _value = default;
            }

            internal readonly Func<Result<TOut>> _func;
            internal readonly TOut _value;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace MimiTools.Tools
{
    public interface IAwaitable
    {
        IAwaiter GetAwaiter();
    }

    public interface IAwaitable<TResult>
    {
        IAwaiter<TResult> GetAwaiter();
    }

    public interface ICustomAwaitable<TAwaiter> where TAwaiter : IAwaiter
    {
        TAwaiter GetAwaiter();
    }

    public interface ICustomAwaitable<TAwaiter, TResult> where TAwaiter : IAwaiter<TResult>
    {
        TAwaiter GetAwaiter();
    }

    public interface IAwaiter : INotifyCompletion
    {
        bool IsCompleted { get; }

        void GetResult();
    }

    public interface IAwaiter<TResult> : INotifyCompletion
    {
        bool IsCompleted { get; }

        TResult GetResult();
    }
}

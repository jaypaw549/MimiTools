using System;
using System.Collections.Generic;
using System.Text;

namespace MimiTools.Sync
{
    public struct ThreadSafe32
    {
        private Barrier32 _barrier;

        public void Do(Action action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            Barrier32.Pass pass = _barrier.Enter();
            try
            {
                action.Invoke();
            }
            finally
            {
                _barrier.Exit(pass);
            }
        }

        public void Do<T>(Action<T> action, T arg)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            Barrier32.Pass pass = _barrier.Enter();
            try
            {
                action.Invoke(arg);
            }
            finally
            {
                _barrier.Exit(pass);
            }
        }

        public T Do<T>(Func<T> func)
        {
            if (func == null)
                throw new ArgumentNullException(nameof(func));

            Barrier32.Pass pass = _barrier.Enter();
            try
            {
                return func.Invoke();
            }
            finally
            {
                _barrier.Exit(pass);
            }
        }

        public TOut Do<TIn, TOut>(Func<TIn, TOut> func, TIn arg)
        {

            if (func == null)
                throw new ArgumentNullException(nameof(func));

            Barrier32.Pass pass = _barrier.Enter();
            try
            {
                return func.Invoke(arg);
            }
            finally
            {
                _barrier.Exit(pass);
            }
        }
    }
}

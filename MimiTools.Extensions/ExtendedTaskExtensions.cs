using System;
using System.Threading;
using System.Threading.Tasks;

namespace MimiTools.Extensions.Tasks
{
    public static class ExtendedTaskExtensions
    {
        public static Task<object> ToObjectTask(this Task t)
        {
            if (t.GetType() == typeof(Task))
                return t.ContinueWith<object>(_ => null, TaskContinuationOptions.ExecuteSynchronously);

            Func<Task, Task<object>> converter = ToObjectTask<object>;
            converter = (Func<Task, Task<object>>)Delegate.CreateDelegate(typeof(Func<Task, Task<object>>),
                converter.Method.GetGenericMethodDefinition().MakeGenericMethod(t.GetType().GetGenericArguments()[0]));

            return converter(t);
        }

        private static async Task<object> ToObjectTask<T>(this Task t)
            => await (Task<T>)t;

        public static Task ToCancelledTask(this CancellationToken t)
            => Task.FromCanceled(t);

        public static Task<T> ToCancelledTask<T>(this CancellationToken t)
            => Task.FromCanceled<T>(t);

        public static Task ToTaskException(this Exception e)
            => Task.FromException(e);

        public static Task<T> ToTaskException<T>(this Exception e)
            => Task.FromException<T>(e);

        public static Task<T> ToTaskResult<T>(this T value)
            => Task.FromResult(value);

        public static void WaitAndUnwrapException(this Task t)
            => t.GetAwaiter().GetResult();

        public static T WaitAndUnwrapException<T>(this Task<T> t)
            => t.GetAwaiter().GetResult();
    }
}

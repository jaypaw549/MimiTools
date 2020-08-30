using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MimiTools.Sync
{
    public static partial class LockExtensions
    {
        public static ILock GetLock(this IAsyncLockable obj, CancellationToken token = default, long timeout = Timeout.Infinite)
            => AwaitRequest(obj.RequestLock(), token, timeout);

        public static async Task<ILock> GetLockAsync(this IAsyncLockable obj, CancellationToken token = default, long timeout = Timeout.Infinite)
            => await AwaitRequestAsync(obj.RequestLock(), token, timeout);

        public static ILock GetSharedLock(this IAsyncShareableLockable obj, CancellationToken token = default, long timeout = Timeout.Infinite)
            => AwaitRequest(obj.RequestSharedLock(), token, timeout);

        public static async Task<ILock> GetSharedLockAsync(this IAsyncShareableLockable obj, CancellationToken token = default, long timeout = Timeout.Infinite)
            => await AwaitRequestAsync(obj.RequestSharedLock(), token, timeout);

        public static IBindableLock GetLock(this IAsyncBindableLockable obj, CancellationToken token = default, long timeout = Timeout.Infinite)
            => AwaitRequest(obj.RequestLock(), token, timeout) as IBindableLock;

        public static async Task<IBindableLock> GetLockAsync(this IAsyncBindableLockable obj, CancellationToken token = default, long timeout = Timeout.Infinite)
            => await AwaitRequestAsync(obj.RequestLock(), token, timeout) as IBindableLock;

        public static IBindableLock GetSharedLock(this IAsyncBindableShareableLockable obj, CancellationToken token = default, long timeout = Timeout.Infinite)
            => AwaitRequest(obj.RequestSharedLock(), token, timeout) as IBindableLock;

        public static async Task<IBindableLock> GetSharedLockAsync(this IAsyncBindableShareableLockable obj, CancellationToken token = default, long timeout = Timeout.Infinite)
            => await AwaitRequestAsync(obj.RequestSharedLock(), token, timeout) as IBindableLock;

        private static ILock AwaitRequest(ILockRequest request, CancellationToken token, long timeout)
        {
            using ManualResetEventSlim mres = new ManualResetEventSlim(false);

            request.OnCompleted(mres.Set);
            using (new Timer(SetEvent, mres, timeout, Timeout.Infinite))
            using (token.Register(() => request.Cancel()))
                mres.Wait();

            if (!request.IsCompleted && request.Cancel())
                throw new TimeoutException();

            return request.GetLock();
        }

        private static async Task<ILock> AwaitRequestAsync(ILockRequest request, CancellationToken token, long timeout)
        {
            using CancellationTokenSource t_source = CancellationTokenSource.CreateLinkedTokenSource(token);

            Task delay = Task.Delay(TimeSpan.FromMilliseconds(timeout), t_source.Token);
            Task wait = request.WaitAsync();

            Task completed;
            using(t_source.Token.Register(() => request.Cancel()))
                completed = await Task.WhenAny(delay, wait);

            if (completed == wait && request.Cancel())
                throw new TimeoutException();

            return request.GetLock();
        }

        private static void SetEvent(object obj)
            => (obj as ManualResetEventSlim)?.Set();
    }
}

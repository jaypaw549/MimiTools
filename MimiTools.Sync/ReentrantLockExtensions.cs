using MimiTools.Tools;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MimiTools.Sync
{
    public static partial class ReentrantLockExtensions
    {
        private static void Bind(ref ReentrantLock.Lock l)
            => l.Bind();

        /// <summary>
        /// Configures the binding status of the lock when acquired. A bound lock cannot be passed between sibling tasks/threads,
        /// but child tasks can also acquire the lock if they want.
        /// </summary>
        /// <param name="task">The task that will return an unbound lock</param>
        /// <param name="bind">whether or not to bind the lock</param>
        /// <returns>an awaitable object that will properly bind the returend lock if configured</returns>
        public static TaskResultFixup<ReentrantLock.Lock> ConfigureBind(this Task<ReentrantLock.Lock> task, bool bind)
            => new TaskResultFixup<ReentrantLock.Lock>(task, bind ? new TaskResultFixupDelegate<ReentrantLock.Lock>(Bind) : Pass);

        public static ConfiguredTaskResultFixup<ReentrantLock.Lock> ConfigureBind(this ConfiguredTaskAwaitable<ReentrantLock.Lock> awaitable, bool bind)
            => new ConfiguredTaskResultFixup<ReentrantLock.Lock>(awaitable, bind ? new TaskResultFixupDelegate<ReentrantLock.Lock>(Bind) : Pass);

        /// <summary>
        /// Waits synchronously for the lock, returning the disposable object when it's when it's acquired.
        /// </summary>
        /// <param name="l">The lock to acquired</param>
        /// <returns>A disposable struct that will release the lock when disposed</returns>
        public static ReentrantLock.Lock GetLock(this ReentrantLock l)
            => l.RequestLock().GetLock();

        public static ReentrantLock.Lock GetLock(this ReentrantLock l, CancellationToken token)
        {
            ReentrantLock.LockRequest request = l.RequestLock();
            using (token.Register(request.Cancel))
                return request.GetLock();
        }

        public static ReentrantLock.Lock GetLock(this ReentrantLock l, long timeout)
        {
            ReentrantLock.LockRequest request = l.RequestLock();
            using (new Timer(Cancel, request, timeout, Timeout.Infinite))
                return request.GetLock();
        }

        public static ReentrantLock.Lock GetLock(this ReentrantLock l, CancellationToken token, long timeout)
        {
            ReentrantLock.LockRequest request = l.RequestLock();
            using (token.Register(request.Cancel))
            using (new Timer(Cancel, request, timeout, Timeout.Infinite))
                return request.GetLock();
        }

        public static async Task<ReentrantLock.Lock> GetLockAsync(this ReentrantLock l)
            => await l.RequestLock();

        public static async Task<ReentrantLock.Lock> GetLockAsync(this ReentrantLock l, CancellationToken token)
        {
            ReentrantLock.LockRequest request = l.RequestLock();
            using (token.Register(request.Cancel))
                return await request;
        }

        public static async Task<ReentrantLock.Lock> GetLockAsync(this ReentrantLock l, long timeout)
        {
            ReentrantLock.LockRequest request = l.RequestLock();
            using (new Timer(Cancel, request, timeout, Timeout.Infinite))
                return await request;
        }

        public static async Task<ReentrantLock.Lock> GetLockAsync(this ReentrantLock l, CancellationToken token, long timeout)
        {
            ReentrantLock.LockRequest request = l.RequestLock();

            using (token.Register(request.Cancel))
            using (new Timer(Cancel, request, timeout, Timeout.Infinite))
                return await request;
        }

        private static void Pass(ref ReentrantLock.Lock l) { }

        public static bool TryGetLock(this ReentrantLock l, CancellationToken token, out ReentrantLock.Lock @lock)
        {
            ReentrantLock.LockRequest request = l.RequestLock();
            using (token.Register(request.Cancel))
                request.Wait();
            
            if (request.IsReady)
            {
                @lock = request.GetLock();
                return true;
            }
            @lock = default;
            return false;
        }

        public static bool TryGetLock(this ReentrantLock l, long timeout, out ReentrantLock.Lock @lock)
        {
            ReentrantLock.LockRequest request = l.RequestLock();
            using (new Timer(Cancel, request, timeout, Timeout.Infinite))
                request.Wait();

            if (request.IsReady)
            {
                @lock = request.GetLock();
                return true;
            }
            @lock = default;
            return false;
        }

        public static bool TryGetLock(this ReentrantLock l, CancellationToken token, long timeout, out ReentrantLock.Lock @lock)
        {
            ReentrantLock.LockRequest request = l.RequestLock();
            using (token.Register(request.Cancel))
            using (new Timer(Cancel, request, timeout, Timeout.Infinite))
                request.Wait();

            if (request.IsReady)
            {
                @lock = request.GetLock();
                return true;
            }
            @lock = default;
            return false;
        }

        public static async Task<bool> TryGetLockAsync(this ReentrantLock l, CancellationToken token, Action<ReentrantLock.Lock> on_acquired)
        {
            ReentrantLock.LockRequest request = l.RequestLock();
            using (token.Register(request.Cancel))
                await request.WaitAsync();

            if (request.IsReady)
            {
                on_acquired(request.GetLock());
                return true;
            }
            return false;
        }

        public static async Task<bool> TryGetLockAsync(this ReentrantLock l, long timeout, Action<ReentrantLock.Lock> on_acquired)
        {
            ReentrantLock.LockRequest request = l.RequestLock();
            using (new Timer(Cancel, request, timeout, Timeout.Infinite))
                await request.WaitAsync();

            if (request.IsReady)
            {
                on_acquired(request.GetLock());
                return true;
            }
            return false;
        }

        public static async Task<bool> TryGetLockAsync(this ReentrantLock l, CancellationToken token, long timeout, Action<ReentrantLock.Lock> on_acquired)
        {
            ReentrantLock.LockRequest request = l.RequestLock();
            using (token.Register(request.Cancel))
            using (new Timer(Cancel, request, timeout, Timeout.Infinite))
                await request.WaitAsync();

            if (request.IsReady)
            {
                on_acquired(request.GetLock());
                return true;
            }
            return false;
        }

        private static void Cancel(object obj)
            => (obj as ReentrantLock.LockRequest)?.Cancel();
    }
}

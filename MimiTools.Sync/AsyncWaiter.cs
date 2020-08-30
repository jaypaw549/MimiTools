using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MimiTools.Sync
{
    /// <summary>
    /// A class that abuses <see cref="AsyncLock"/> and allows synchronous and asynchronous waiting of signals from other threads.
    /// For passing objects or values between threads, see <see cref="AsyncDispatcher{T}" />.
    /// This class is thread-safe (probably)
    /// </summary>
    public sealed class AsyncWaiter
    {
        public AsyncWaiter()
        {
            _next = _sync.GetLock().Id;
        }

        /// <summary>
        /// The lock responsible for holding up waiting threads/tasks
        /// </summary>
        private readonly AsyncLock _sync = new AsyncLock();

        /// <summary>
        /// The current lock for <see cref="_sync"/>, Release to let a single waiter to stop waiting.
        /// </summary>
        private volatile int _next;

        /// <summary>
        /// The signal balance, positive if there are outstanding signals.
        /// </summary>
        private int _signals = 0;

        /// <summary>
        /// Sends a single non-future signal, letting the next task or thread in line continue execution
        /// </summary>
        /// <returns>true if we signaled a task or thread successfully</returns>
        public bool Signal()
            => Signal(1, false) == 1;

        /// <summary>
        /// Sends a single signal, optionally storing it if there's no waiters, so that future waiters can recieve them.
        /// </summary>
        /// <param name="future">whether or not to store the signal if there's nothing to signal at the time</param>
        /// <returns>false if we couldn't signal anything. True if we stored a signal or signaled a waiting thread or task successfully</returns>
        public bool Signal(bool future)
            => Signal(1, future) == 1;

        /// <summary>
        /// Sends the specified amount of signals to enqueued waters.
        /// </summary>
        /// <param name="count">The number of signals to send</param>
        /// <returns>The number of tasks and threads that recieved the signal.</returns>
        public int Signal(int count)
            => Signal(count, false);

        /// <summary>
        /// Sends the specified amount of signals. If future is false, tries to limit the number of signals to the number of waiters.
        /// This is not guaranteed to work.
        /// </summary>
        /// <param name="count">The number of signals to send</param>
        /// <param name="future">Whether or not to store the excess signals</param>
        /// <returns>How many signals were sent and/or stored</returns>
        public int Signal(int count, bool future)
        {
            if (!future)
            {
                int waiting = _sync.WaitQueue - Volatile.Read(ref _signals);
                if (waiting < count)
                    count = waiting;
            }

            if (count <= 0)
                return 0;

            Interlocked.Add(ref _signals, count);
            if (_sync.TryGetCurrentLock(_next, out AsyncLock.Lock l))
                l.Release();

            return count;
        }
        
        /// <summary>
        /// Sends a number of signals equal to the number of waiters in queue at the start of execution of this method.
        /// </summary>
        public void SignalAll()
            => Signal(_sync.WaitQueue, false);

        /// <summary>
        /// Attempts to consume a signal, used to check if we need to block at all.
        /// </summary>
        /// <returns>True if we can return right away.</returns>
        private bool TryConsumeSignal()
        {
            int signals = Volatile.Read(ref _signals);
            while (signals > 0)
            {
                int ret = Interlocked.CompareExchange(ref _signals, signals - 1, signals);
                if (ret == signals)
                    return true;
                signals = ret;
            }
            return false;
        }

        /// <summary>
        /// Attempts to pass the signal on, used to simplify the process of signaling multiple tasks and/or threads.
        /// </summary>
        /// <param name="l">The lock that we will release as our signal to the next thread or task in line.</param>
        private void TryPassSignal(AsyncLock.Lock l)
        {
            _next = l.Id;

            if (Interlocked.Decrement(ref _signals) > 0)
                l.Release();
        }

        /// <summary>
        /// Waits for a signal synchronously.
        /// </summary>
        public void Wait()
        {
            if (TryConsumeSignal())
                return;

            TryPassSignal(_sync.GetLock());

        }

        /// <summary>
        /// Waits for a signal asynchronously.
        /// </summary>
        /// <returns>A task that completes when a signal is received</returns>
        public async Task WaitAsync()
        {
            if (TryConsumeSignal())
                return;

            TryPassSignal(await _sync.GetLockAsync());
            await Task.Yield();
        }
    }
}
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MimiTools.Sync
{
    /// <summary>
    /// A class that really abuses SyncLock, and allows synchronous and asynchronous waiting of signals from other threads. Future implementations could allow for passing objects between threads.
    /// 
    /// This class itself works by creating a SyncLock object for every call to wait, and enqueing the lock into the queue for eventual release. It then tries to reacquire the lock for the amount
    /// of time specified, and if it's successful it'll signal it's successful. If not it'll release the lock, making it get passed over in the queue when signals are sent.
    /// 
    /// This class, like SyncLock, is theoretically thread-safe, however it uses SyncLock to maintain that thread safety and is therefore reliant on its thread safety.
    /// </summary>
    public class AsyncWaiter
    {
        /// <summary>
        /// The lock to make sure that our queue works appropriately.
        /// </summary>
        private readonly SyncLock _sync = new SyncLock();

        private readonly Queue<DisposableWrapper> queue = new Queue<DisposableWrapper>();

        private int pulses = 0;

        public int GetQueueLength()
        {
            using (_sync.GetLock())
                return queue.Count;
        }

        public async Task<int> GetQueueLengthAsync()
        {
            using (await _sync.GetLockAsync().ConfigureAwait(false))
                return queue.Count;
        }

        public void Pulse(bool future_pulse = false)
            => PulseMany(1, future_pulse);

        public void PulseAll()
        {
            using (_sync.GetLock())
            {
                while (queue.Count > 0)
                    queue.Dequeue().Dispose();
            }
        }

        public async Task PulseAllAsync()
        {
            using (await _sync.GetLockAsync())
            {
                while (queue.Count > 0)
                    queue.Dequeue().Dispose();
            }
        }

        public Task PulseAsync(bool future_pulse = false)
            => PulseManyAsync(1, future_pulse);

        public void PulseMany(int count, bool future_pulse = false)
        {
            using (_sync.GetLock())
            {
                while (queue.Count > 0 && count > 0)
                    if (queue.Dequeue().Dispose())
                        count--;

                if (count > 0 && future_pulse)
                    pulses += count;
            }
        }

        public async Task PulseManyAsync(int count, bool future_pulse = false)
        {
            using (await _sync.GetLockAsync())
            {
                while (queue.Count > 0 && count > 0)
                    if (queue.Dequeue().Dispose())
                        count--;

                if (count > 0 && future_pulse)
                    pulses += count;
            }
        }

        /// <summary>
        /// A wrapper method for the other method, Creates a cancellation token that expires after the specified timeout.
        /// </summary>
        /// <param name="timeout">How long to wait for a signal in milliseconds</param>
        /// <returns>True, if a signal was received in time. False if the operation timed out</returns>
        public bool Wait(int timeout = -1)
        {
            using (CancellationTokenSource source = new CancellationTokenSource(timeout))
                return Wait(source.Token);
        }

        /// <summary>
        /// Waits for a signal from another thread, aborting if the operation is cancelled
        /// </summary>
        /// <param name="token"></param>
        /// <returns>True, if the operation succeeds. False if the operation is cancelled</returns>
        public bool Wait(CancellationToken token)
        {
            //Create a new lock to wait for
            SyncLock blocker;

            //Then acquires the lock and wraps it up in preperation to offering it to the queue
            DisposableWrapper wrapper;

            //Next acquires the lock for this instance, and enqueues our wrapped lock
            using (_sync.GetLock())
            {
                if (pulses > 0)
                {
                    pulses--;
                    return true;
                }
                blocker = new SyncLock();
                queue.Enqueue(wrapper = new DisposableWrapper(blocker.GetLock()));
            }

            //Finally, waits for the lock, and if we time out, we release it ourselves. 
            //If on the rare chance the lock is released between the time we stop waiting and the time we try to release it, say we were signalled successfully
            if (!blocker.TryGetLock(token, out IDisposable @lock))
                return !wrapper.Dispose(); //Just in case a last minute call lets us return true;

            //If we did get a lock however, release that
            @lock.Dispose();

            //Before finally signaling that we were successfuly
            return true;
        }

        /// <summary>
        /// A wrapper method for the other method, Creates a cancellation token that expires after the specified timeout.
        /// </summary>
        /// <param name="timeout">How long to wait for a signal in milliseconds</param>
        /// <returns>True, if a signal was received in time. False if the operation timed out</returns>
        public async Task<bool> WaitAsync(int timeout = -1)
        {
            using (CancellationTokenSource source = new CancellationTokenSource(timeout))
                return await WaitAsync(source.Token);
        }

        /// <summary>
        /// Asynchronously waits for a signal from another thread, aborting if the operation is cancelled
        /// </summary>
        /// <param name="token"></param>
        /// <returns>True, if the operation succeeds. False if the operation is cancelled</returns>
        public async Task<bool> WaitAsync(CancellationToken token)
        {
            //The SyncLock to use
            SyncLock blocker;

            //The lock wrapper
            DisposableWrapper wrapper;

            //Next acquires the lock for this instance, and enqueues our wrapped lock
            using (await _sync.GetLockAsync().ConfigureAwait(false))
            {
                //If we have leftover pulses, consume one instead of waiting.
                if (pulses > 0)
                {
                    pulses--;
                    return true;
                }

                //Otherwise create the SyncLock
                blocker = new SyncLock();

                //And wrap up a lock from it for eventual release
                queue.Enqueue(wrapper = new DisposableWrapper(await blocker.GetLockAsync()));
            }

            //Finally, waits for the lock, and if we time out, we release it ourselves. 
            //If on the rare chance the lock is released between the time we stop waiting and the time we try to release it, say we were signalled successfully
            if (!await blocker.TryGetLockAsync(token, l => l.Dispose()))
                return !wrapper.Dispose();

            //say we were signalled successfully.
            return true;
        }

        /// <summary>
        /// A private class to track whether or not we have already disposed of our lock.
        /// </summary>
        private class DisposableWrapper
        {
            internal DisposableWrapper(IDisposable target)
            {
                Target = target;
            }

            private volatile IDisposable Target;

            /// <summary>
            /// Disposes of the lock
            /// </summary>
            /// <returns>whether or not the lock has already been disposed of</returns>
            internal bool Dispose()
            {
                IDisposable target = Interlocked.Exchange(ref Target, null);
                target?.Dispose();
                return target != null;
            }
        }
    }
}
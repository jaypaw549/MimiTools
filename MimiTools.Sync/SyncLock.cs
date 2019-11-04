using System;
using System.Threading;
using System.Threading.Tasks;

namespace MimiTools.Sync
{
    /// <summary>
    /// A class for synchronizing threads. Each function returns, or can return a disposable object. While that disposable object remains undisposed of, subsequent
    /// calls to these functions will block the calling thread or suspend the active task until the object is disposed of. Then the next caller is given a new disposable object
    /// which does the same thing. This repeats until there are no callers. This disposable object is referred to as the lock.
    /// </summary>
    public class SyncLock
    {
        /// <summary>
        /// The last lock in the pseudo-queue. The head of the queue would be the currently held lock
        /// </summary>
        protected volatile Lock _last = null;

        /// <summary>
        /// Whether or not a call to any of the functions would return a lock immediately. Recommended that if you want to try for a lock without waiting,
        /// that you use GetLockIfAvailable() instead of checking this property and calling one of the other functions.
        /// </summary>
        public virtual bool Available { get => !_last?.Alive ?? true; }

        /// <summary>
        /// Gets the lock, and returns it as a disposable object. Disposing of it will release the lock, letting the next thread/task acquire it
        /// </summary>
        /// <returns>A disposable object representing the lock</returns>
        /// <example>
        /// using(Lock.GetLock())
        /// {
        ///     //Synchronous code here
        /// }
        /// </example>
        public IDisposable GetLock()
        {
            if (!TryGetLock(Timeout.InfiniteTimeSpan, CancellationToken.None, out IDisposable @lock))
                throw new TimeoutException("Operation (somehow) timed out!");

            return @lock;
        }

        /// <summary>
        /// Gets the lock asynchronously, and returns it as a disposable object. Disposing of it will release the lock, letting the next thread/task acquire it
        /// </summary>
        /// <returns>A disposable object representing the lock</returns>
        /// <example>
        /// using(await Lock.GetLockAsync())
        /// {
        ///     //Synchronous code here
        /// }
        /// </example>
        public async Task<IDisposable> GetLockAsync()
        {
            IDisposable @lock = null;
            if (!await TryGetLockAsync(Timeout.InfiniteTimeSpan, CancellationToken.None, l => @lock = l))
                throw new TimeoutException("Operation (somehow) timed out!");

            return @lock;
        }

        /// <summary>
        /// Gets the lock if it's available. There is no asynchronous version of this as there's no blocking done in this.
        /// </summary>
        /// <param name="lock">The lock represented as a disposable object</param>
        /// <returns>Whether or not it was able to acquire the lock.</returns>
        public virtual bool GetLockIfAvailable(out IDisposable @lock)
        {
            //Creates a new lock, that we *might* return
            Lock current = new Lock();

            //Exchanges the two locks to put it this lock into the queue, hopefully at the head
            Lock prev = Interlocked.Exchange(ref _last, current);

            //If the previous lock is dead, the we *are* the head, so set our lock and return true.
            if (!prev?.Alive ?? true)
            {
                @lock = current;
                return true;
            }

            //Otherwise we aren't and we want to bail, so attempt to put the previous back in place
            if (current != Interlocked.CompareExchange(ref _last, prev, current))
                //We weren't able to so perform a transfer instead
                current.Transfer(prev);

            //We failed to get the lock without waiting, so return false
            @lock = null;
            return false;
        }

        /// <summary>
        /// Waits for the lock for the specified amount of time
        /// </summary>
        /// <param name="timeout">How long to wait for the lock</param>
        /// <param name="lock">The lock object, if the operation didn't time out</param>
        /// <returns>true if it was able to get the lock in time, otherwise false</returns>
        /// <remarks>If you're going to pass an infinite timeout, it's better to call GetLock()</remarks>
        /// <remarks>If you're going to pass a zero timeout, it's better to call GetLockIfAvailable()</remarks>
        public bool TryGetLock(TimeSpan timeout, out IDisposable @lock)
            => TryGetLock(timeout, CancellationToken.None, out @lock);

        /// <summary>
        /// Waits for the lock until the operation is cancelled
        /// </summary>
        /// <param name="token">The token indicating if the operation is cancelled</param>
        /// <param name="lock">The lock object, if the operation isn't cancelled before retrieval</param>
        /// <returns>true if it got the lock, false if the operation was cancelled</returns>
        public bool TryGetLock(CancellationToken token, out IDisposable @lock)
            => TryGetLock(Timeout.InfiniteTimeSpan, token, out @lock);

        /// <summary>
        /// Waits for the lock for the specified amount of time, or until the operation is cancelled, whichever's first
        /// </summary>
        /// <param name="timeout">How long to wait for the lock</param>
        /// <param name="token">The token to check for cancellation with</param>
        /// <param name="lock">The lock object, if the operation completes successfully</param>
        /// <returns>true, if got the lock, false if the operation timed out or was cancelled</returns>
        /// <remarks>If you're going to pass a zero timeout, it's better to call GetLockIfAvailable()</remarks>
        public virtual bool TryGetLock(TimeSpan timeout, CancellationToken token, out IDisposable @lock)
        {
            //Quick check, for consistent outcomes
            if (token.IsCancellationRequested)
            {
                @lock = null;
                return false;
            }

            //Create a new lock to enter in to the queue
            Lock current = new Lock();

            //Add the lock into the queue
            Lock prev = Interlocked.Exchange(ref _last, current);

            if (prev != null)
            {
                prev.OnTransfer(on_transfer);
                prev.Transfer(prev);

                void on_transfer(Lock l)
                {
                    prev = l;
                }
            }

            //If we have to wait for our turn
            if (prev?.Alive ?? false)
            {
                //Flag to determine if we received the signal or not
                bool success = false;

                //Prepare the manual cancellation source
                using (CancellationTokenSource source = CancellationTokenSource.CreateLinkedTokenSource(token))
                {
                    //Create thread-based signal object
                    object obj = new object();

                    lock (obj)
                    {
                        //Proxy flag,
                        bool signal = false;

                        //Set up the cancel signal
                        source.Token.Register(() =>
                        {
                            lock (obj)
                                Monitor.PulseAll(obj);
                        });

                        //Set up the ready signal
                        prev.SetNext(() =>
                        {
                            lock (obj)
                            {
                                signal = true;
                                Monitor.PulseAll(obj);
                            }
                        });

                        //Make sure we didn't miss the signal already
                        if (prev.Alive)
                        {
                            if (!token.IsCancellationRequested)
                            {
                                //Wait for the signal, or just timeout
                                Monitor.Wait(obj, timeout, true);
                                //Set our success equal to if we got the signal
                                success = signal;
                            }
                            else
                                success = false;

                        }
                        else
                            success = !token.IsCancellationRequested;
                    }
                }

                //If we didn't get the signal before the timeout, fire the transfer signal
                if (!success)
                {
                    if (current != Interlocked.CompareExchange(ref _last, prev, current))
                        current.Transfer(prev);
                    @lock = null;
                    return false;
                }
            }
            else if (token.IsCancellationRequested)
            {
                if (current != Interlocked.CompareExchange(ref _last, prev, current))
                    current.Transfer(prev);
                @lock = null;
                return false;
            }

            @lock = current;
            return true;
        }

        /// <summary>
        /// Tries to get the lock asynchronously, waiting only the specified amount of time for it. Calls the passed function if it acquires the lock.
        /// Take special care to dispose of the lock afterwards, as it is not automatically disposed of when the passed function returns.
        /// </summary>
        /// <param name="timeout">How long to wait for the lock</param>
        /// <param name="on_success">Function to call when the lock is acquired, the lock is passed as the parameter to the function.</param>
        /// <returns>true, if the lock was acquired, otherwise false</returns>
        /// <remarks>If you're going to pass an infinite timeout, it's better to call GetLock()</remarks>
        /// <remarks>If you're going to pass a zero timeout, it's better to call GetLockIfAvailable()</remarks>
        public Task<bool> TryGetLockAsync(TimeSpan timeout, Action<IDisposable> on_success)
            => TryGetLockAsync(timeout, CancellationToken.None, on_success);

        /// <summary>
        /// Tries to get the lock asynchronously, waiting until the operation is cancelled. Calls the passed function if it acquires the lock.
        /// Take special care to dispose of the lock afterwards, as it is not automatically disposed of when the passed function returns.
        /// </summary>
        /// <param name="token">The token to check for cancellation with</param>
        /// <param name="on_success">The function to call when the lock is acquired, the lock is passed as the parameter to the function</param>
        /// <returns>true if the lock was acquired, otherwise false</returns>
        /// <remarks>If you're going to pass an infinite timeout, it's better to call GetLock()</remarks>
        /// <remarks>If you're going to pass a zero timeout, it's better to call GetLockIfAvailable()</remarks>
        public Task<bool> TryGetLockAsync(CancellationToken token, Action<IDisposable> on_success)
            => TryGetLockAsync(Timeout.InfiniteTimeSpan, token, on_success);

        /// <summary>
        /// Tries to get the lock asynchronously, waiting the specified amount of time for it, or until the operation is cancelled. Calls the passed function if it acquires the lock.
        /// Take special care to dispose of the lock afterwards, as it is not automatically disposed of when the passed function returns.
        /// </summary>
        /// <param name="timeout">How long to wait for the lock</param>
        /// <param name="token">The token to check for cancellation with</param>
        /// <param name="on_success">The function to call when the lock is acquired, the lock is passed as the parameter to the function</param>
        /// <returns>true if the lock was acquired, otherwise false</returns>
        /// <remarks>If you're going to pass a zero timeout, it's better to call GetLockIfAvailable()</remarks>
        public virtual async Task<bool> TryGetLockAsync(TimeSpan timeout, CancellationToken token, Action<IDisposable> on_success)
        {
            //Quick check, for consistent outcomes
            if (token.IsCancellationRequested)
                return false;

            //Create a new lock to enter in to the queue
            Lock current = new Lock();

            //Add the lock into the queue
            Lock prev = Interlocked.Exchange(ref _last, current);

            //If there's a previous lock
            if (prev != null)
            {
                //Prepare for any transfers that might take place
                prev.OnTransfer(on_transfer);

                //Force-initiate a transfer to itself to initiate the filter
                prev.Transfer(prev);

                void on_transfer(Lock l)
                {
                    prev = l;
                }
            }

            //If we have to wait for our turn
            if (prev?.Alive ?? false)
            {
                //Create task-based signal object
                TaskCompletionSource<bool> source = new TaskCompletionSource<bool>();

                //Set up the ready signal
                prev.SetNext(() => source.TrySetResult(true));

                //Make sure we didn't miss the signal already
                if (prev.Alive)
                {
                    //enable manual cancelling so we can abort the delay if we get the signal... save system resources
                    using (CancellationTokenSource cancel = CancellationTokenSource.CreateLinkedTokenSource(token))
                    {
                        //Wait for either timeout or the signal
                        await Task.WhenAny(source.Task, Task.Delay(timeout, token)).ConfigureAwait(false);

                        //Cancel the delay task if not already finished
                        cancel.Cancel();
                    }

                    //If we didn't get the signal before the timeout, fire the transfer signal
                    if (!source.Task.IsCompleted)
                    {
                        if (current != Interlocked.CompareExchange(ref _last, prev, current))
                            current.Transfer(prev);
                        return false;
                    }
                }
            }
            else if (token.IsCancellationRequested)
            {
                if (current != Interlocked.CompareExchange(ref _last, prev, current))
                    current.Transfer(prev);
                return false;
            }

            on_success(current);
            return true;
        }

        protected sealed class Lock : IDisposable
        {
            private volatile bool _alive;
            private volatile Lock transfer;
            private volatile Action next;
            private volatile Action<Lock> on_transfer;

            internal bool Alive { get => _alive; }

            internal Lock()
            {
                _alive = true;
                next = null;
                on_transfer = null;
            }

            public void Dispose()
            {
                _alive = false;
                Interlocked.Exchange(ref next, null)?.Invoke();
            }

            internal void OnTransfer(Action<Lock> a)
                => on_transfer = a;

            internal void SetNext(Action a)
                => next = a;

            //Transfers all values of this lock to the target lock, and modifies all references to this lock to the target lock.
            //When this function completes, this lock should be ready for garbage collection, and any references to this should now
            //point to the next living lock up the chain. (in other words, all transferred locks are traversed until we reach the destination
            internal void Transfer(Lock target)
            {
                do
                {
                    //Pre-filtering
                    while (target.transfer != null)
                        target = target.transfer;

                    //Cycle prevention
                    if (target == this)
                        return;

                    //Mark ourselves as a transfer
                    transfer = target;

                    //Invoke the transfer event so our waiting thread can use the up-to-date lock
                    on_transfer?.Invoke(target);

                    //Set fields
                    if (next != null)
                        target.next = next;

                    if (on_transfer != null)
                        target.on_transfer = on_transfer;

                    //If our target isn't alive, send the dispose signal
                    if (!target.Alive)
                        Dispose();

                } while (target.transfer != null); //Loop because this *could* change at any time
            }
        }
    }

    public class PrototypeSyncLock
    {
        protected volatile LockRequest _internal_last = new LockRequest(null);
        protected volatile LockRequest _public_last = new LockRequest(null);

        public bool GetLockIfAvailable(out IDisposable @lock)
        {
            @lock = null;
            using (GetInternalLock())
            {
                if (_public_last.Disposed)
                {
                    @lock = _public_last = new LockRequest(null);
                    return true;
                }
            }
            return false;
        }

        public bool TryGetLock(TimeSpan timeout, CancellationToken token, out IDisposable @lock)
        {
            object monitor = new object();
            Task timer = Task.Delay(timeout, token).ContinueWith(t => { lock (monitor) { Monitor.Pulse(monitor); } }, TaskContinuationOptions.ExecuteSynchronously);
            @lock = null;

            LockRequest current, previous;

            using (GetInternalLock())
            {
                previous = _public_last;
                current = _public_last = new LockRequest(() => { lock (monitor) { Monitor.Pulse(monitor); } });
            }

            lock (monitor)
                if (previous.SetNext(_public_last))
                    Monitor.Wait(monitor);

            if (timer.IsCompleted)
            {
                bool passed;
                using (GetInternalLock())
                    if (passed = current.PassNext(previous))
                        if (_public_last == current)
                            _public_last = previous;

                if (!passed)
                    current.Dispose();

                return false;
            }

            @lock = current;
            return true;
        }

        private LockRequest GetInternalLock()
        {
            object monitor = new object();
            LockRequest current = new LockRequest(() => { lock (monitor) Monitor.Pulse(monitor); });
            LockRequest prev = Interlocked.Exchange(ref _internal_last, current);

            lock (monitor)
                if (prev.SetNext(current))
                    Monitor.Wait(monitor);

            return current;
        }

        private async Task<LockRequest> GetInternalLockAsync()
        {
            TaskCompletionSource<object> monitor = new TaskCompletionSource<object>();

            LockRequest current = new LockRequest(() => monitor.SetResult(null));
            LockRequest prev = Interlocked.Exchange(ref _internal_last, current);

            if (prev?.SetNext(current) ?? false)
            {
                await monitor.Task;
                await Task.Yield();
            }

            return current;
        }

        protected class LockRequest : IDisposable
        {
            protected internal LockRequest(Action notify)
            {
                _notify = notify;
            }

            private volatile LockRequest _next;
            private volatile Action _notify;

            protected internal bool Disposed { get => _notify == null; }

            protected internal bool PassNext(LockRequest prev)
                => prev.SetNext(_next);

            protected internal bool SetNext(LockRequest request)
            {
                _next = request;
                return _notify != null;
            }

            public virtual void Dispose()
            {
                if (Interlocked.Exchange(ref _notify, null) != null)
                    Interlocked.Exchange(ref _next, null)?._notify?.Invoke();
            }
        }
    }
}

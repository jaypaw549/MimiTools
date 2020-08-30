using MimiTools.Tools;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace MimiTools.Sync
{
    /// <summary>
    /// A class for synchronizing threads. Each function returns, or can return a disposable object. While that disposable object remains undisposed of, subsequent
    /// calls to these functions will block the calling thread or suspend the active task until the object is disposed of. Then the next caller is given a new disposable object
    /// which does the same thing. This repeats until there are no callers. This disposable object is referred to as the lock. It seems unlikely that I'll be able to make
    /// a struct version of this class, as the async framework requires you do callbacks to notify asynchronous waiters, and delegates themselves are objects.
    /// </summary>
    public sealed class AsyncLock : IAsyncLockable
    {
        /// <summary>
        /// The current active ID, a lock object with this ID is capable of releasing the lock. A lock request with this ID stops being blocked.
        /// This value is guaranteed to be up to date, as it is updated immediately and atomically
        /// </summary>
        private volatile int _current = 0;

        /// <summary>
        /// The next unused ID, any new requests will take this as their ID and increment the value.
        /// This value is guaranteed to be up to date, as it is updated immediately and atomically
        /// </summary>
        private volatile int _free = 0;

        /// <summary>
        /// The next <i>asynchronous</i> request in line. If current matches this when the lock is released, this waiter is notified that is now has the lock.
        /// This value is not guaranteed to be up to date, as its updates are delayed, and are not atomic. 
        /// However it is guaranteed that the request ID will be greater than or equal to <see cref="_current"/>.
        /// All items in the chain are guaranteed to be in order from lowest ID to highest ID.
        /// </summary>
        private LockWaiter _waiter = null;

        /// <summary>
        /// The last <i>asynchronous</i> request in line. Is guaranteed to <i>eventually</i> be up to date. This exists mostly to make enqueing requests as fast as possible.
        /// This value is not guaranteed to be up to date, as its updates are delayed, and are not atomic. 
        /// However it is guaranteed that the request ID will be less than <see cref="_free"/>.
        /// All items in the chain are guaranteed to be in order from lowest ID to highest ID.
        /// This is not guaranteed to be the last item in the chain unless all async method calls to this instance have been completed.
        /// </summary>
        private LockWaiter _last = null;

        /// <summary>
        /// Tells whether or not the lock is free. There is no guarantee it'll remain free even after this call. If you want to acquire the lock <i>only</i> if it's free
        /// use <see cref="TryGetLock(out Lock)"/>, it's fast, non-blocking, and checks if someone has the lock before taking it.
        /// </summary>
        public bool IsAvailable => _current == _free;

        /// <summary>
        /// Returns the number of requests waiting for the lock
        /// </summary>
        public int WaitQueue
        {
            get
            {
                int total = _free - _current - 1;
                if (total < 0)
                    return 0;
                return total;
            }
        }

        /// <summary>
        /// Synchronously gets the lock. Ultimately puts the thread into a SpinWait loop until it's our turn.
        /// </summary>
        /// <returns>A lock guaranteed to be held only by the caller.</returns>
        public Lock GetLock()
            => GetLock(Interlocked.Increment(ref _free) - 1);

        /// <summary>
        /// Synchronously gets the lock with the specified ID, Ultimately puts the thread into a SpinWait loop until it's that ID's turn.
        /// Is internal implementation meant for <see cref="LockWaiter.GetResult"/> calls and <see cref="GetLock"/>
        /// </summary>
        /// <param name="id">The ID to fetch a lock for</param>
        /// <returns>A lock guaranteed to be held only by the caller</returns>
        private Lock GetLock(int id)
        {
            SpinWait wait = new SpinWait();
            while (id != _current)
                wait.SpinOnce();

            return new Lock(this, id);
        }

        /// <summary>
        /// Asynchronously gets the lock, if the lock is immediately available, it returns the lock synchronously, otherwise it puts the request
        /// into the queue and returns a task that waits for the signal that it's free.
        /// </summary>
        /// <returns>A task that will eventually hold the lock</returns>
        public async Task<Lock> GetLockAsync()
        {
            LockWaiter last = Volatile.Read(ref _last); //Since we... in a thread-safe manner get our ID after, the last waiter will always be below our ID at this point in time.
            int id = Interlocked.Increment(ref _free) - 1; //Fetch a unique ID.

            if (id == _current) //If our ID is marked as free, skip all the tedious stuff and return the lock synchronously.
                return new Lock(this, id);

            LockWaiter waiter = new LockWaiter(this, id); //Create a waiter that will be notified when it's our turn.

            if (last == null) //We don't know if we come after another waiter or not, so we'll start from the beginning.
            {
                if (null == LockWaiter.Insert(ref _waiter, waiter)) //We know that at this moment, nothing comes after us, so try and set ourselves to last
                    TryUpdateLast(waiter);
            }

            else //We know that the one we have will *always* come before us, so start there.
            {
                /*
                 * We use a cached version of _last because...
                 * 1) Modifying _last directly could result in the chain being broken if a request with a higher ID gets written there before we try to insert
                 * 2) Even if we modify the next field of _last, there's a chance we'll be out of order anyways.
                 * 3) We got our cached version from a point in time we knew it came before us, so we can safely insert ourselves somewhere *after* it.
                */
                if (null == LockWaiter.Insert(ref last.GetNextRef(), waiter))
                    TryUpdateLast(waiter);
            }

            Lock l;
            using (waiter)
                l = await waiter;
            await Task.Yield();

            return l;
        }

        private bool IsValid(int id)
            => _current == id;

        /// <summary>
        /// Releases the lock if the provided ID matches the current ID. Only accessed by the <see cref="Lock"/> class.
        /// </summary>
        /// <param name="id">The ID of the lock requesting its release</param>
        private bool Release(int id)
        {
            if (id == Interlocked.CompareExchange(ref _current, id + 1, id)) //If we match the currently held ID, increment it to allow the next person to control the lock.
            {
                LockWaiter waiter = Volatile.Read(ref _waiter);

                //Try to notify the next asynchronous request. We will be ignored if it's not their turn.
                if (waiter?.TryDoContinuation() ?? false)
                    waiter.Dispose();

                return true;
            }
            return false;
        }

        /// <summary>
        /// Tries to retrieve the current lock from the specified ID.
        /// </summary>
        /// <param name="request_id">The ID of the request for the lock</param>
        /// <param name="l">The field to write the lock to</param>
        /// <returns>true if the ID matches the current lock, otherwise false</returns>
        public bool TryGetCurrentLock(int request_id, out Lock l)
        {
            int cid = _current;
            if (cid == request_id && cid != _free)
            {
                l = new Lock(this, request_id);
                return true;
            }

            l = default;
            return false;
        }

        /// <summary>
        /// Attempts to get the lock, succeeds only if the lock is free. As this method doesn't insert itself into the queue unless it has acquired the lock,
        /// it has a lower priority than <see cref="GetLock"/> and <see cref="GetLockAsync"/>
        /// </summary>
        /// <param name="l">The variable to store the lock to</param>
        /// <returns>true if the lock was acquired, false if it was already taken</returns>
        public bool TryGetLock(out Lock l)
        {
            //Get what our ID would be if we called GetLock()
            int id = _free;

            //If the lock is ours, attempt to claim the id, and if that's successful, return the lock
            if (_current == id && id == Interlocked.CompareExchange(ref _free, id + 1, id))
            {
                l = new Lock(this, id);
                return true;
            }

            //The lock is claimed by someone else, or the ID was claimed by someone else while we were checking, operation failed.
            l = default;
            return false;
        }

        /// <summary>
        /// Attempts to update <see cref="_last"/> as multiple callers can be in this method, we loop until we know we don't need to write it anymore,
        /// either because we wrote to it or because another caller wrote a more up-to-date value to it.
        /// </summary>
        /// <param name="value">Specifies the value to try and set as our last</param>
        private void TryUpdateLast(LockWaiter value)
        {
            LockWaiter last = Volatile.Read(ref _last);
            while (true)
            {
                //If the last value is bigger than us then break: it will never be smaller than us even if we check again. If we successfully updated it, also break as we've done our job
                if (last != null && last.Id - value.Id > 0)
                    return;

                LockWaiter ret = Interlocked.CompareExchange(ref _last, value, last);
                if (ret == last)
                    return;

                last = ret;
            }
        }

        async Task<ILock> IAsyncLockable.GetLockAsync()
            => await GetLockAsync();

        ILock ILockable.GetLock()
            => GetLock();

        ILockRequest IAsyncLockable.RequestLock()

            => throw new NotImplementedException();

        /// <summary>
        /// The lock struct, allows the holder to release the lock.
        /// </summary>
        public readonly struct Lock : ILock
        {
            /// <summary>
            /// The <see cref="AsyncLock"/> that this lock represents.
            /// </summary>
            ///
            private readonly AsyncLock _sync;

            /// <summary>
            /// The ID of the request that obtained this lock. Is used to release the lock.
            /// </summary>
            private readonly int _id;

            /// <summary>
            /// The ID of the request that obtained this lock, can be used to reobtain this lock so long as this lease remains valid.
            /// </summary>
            public int Id => _id;

            public bool IsValid => _sync.IsValid(_id);

            /// <summary>
            /// Creates a <see cref="Lock"/> object that represents an acquired lock.
            /// </summary>
            /// <param name="sync"></param>
            /// <param name="id"></param>
            internal Lock(AsyncLock sync, int id)
            {
                _sync = sync;
                _id = id;
            }

            void IDisposable.Dispose()
                => Release();

            /// <summary>
            /// Requests that the lock be released. The request is ignored if our ID doesn't match the ID that currently holds the lock.
            /// </summary>
            public bool Release()
                => _sync?.Release(_id) ?? false;

            void ILock.Release()
                => Release();
        }

        /// <summary>
        /// The LockWaiter class, Responsible for managing the asynchronous requests for the lock. Chains together to form a pseudo-queue.
        /// The methods that manage the chain are eventually-consistent, in that every method will eventually put the request into its proper place.
        /// </summary>
        private sealed class LockWaiter : ICustomAwaitable<LockWaiter, Lock>, IAwaiter<Lock>, IDisposable
        {
            /// <summary>
            /// Creates a <see cref="LockWaiter"/> instance, which represents an asynchronous request for the lock.
            /// </summary>
            /// <param name="sync">The lock we're requesting</param>
            /// <param name="id">The ID of our request</param>
            internal LockWaiter(AsyncLock sync, int id)
            {
                _sync = sync;
                _id = id;
            }

            /// <summary>
            /// The ID of our lock request
            /// </summary>
            private readonly int _id;

            internal int Id => _id;

            /// <summary>
            /// The lock we're asynchronously requesting.
            /// </summary>
            private readonly AsyncLock _sync;

            /// <summary>
            /// the actions to perform when we've acquired the lock.
            /// </summary>
            private volatile Action _continuation;

            /// <summary>
            /// the next waiter in line for the lock.
            /// </summary>
            private LockWaiter _next;

            /// <summary>
            /// Required property for the awaitable pattern, tells whether or not this waiter has the lock.
            /// </summary>
            public bool IsCompleted => _sync._current == _id;

            public void Dispose()
            {
                //Mark for redirection while simultantiously fetching the value.
                LockWaiter next = Interlocked.Exchange(ref _next, this);

                //Remove ourselves from the first position, if we're still there.
                _ = Interlocked.CompareExchange(ref _sync._waiter, next, this);

                //Remove ourselves from the last position, if we're still there.
                _ = Interlocked.CompareExchange(ref _sync._last, null, this);
            }

            /// <summary>
            /// Required method for the awaitable pattern, returns itself as its own awaiter.
            /// </summary>
            /// <returns>itself</returns>
            public LockWaiter GetAwaiter()
                => this;

            /// <summary>
            /// Gets a reference to the field that stores the next waiter in line
            /// </summary>
            /// <returns>a reference to the field that stores the next waiter in line</returns>
            internal ref LockWaiter GetNextRef()
            {
                if (_next == this)
                    return ref _sync._waiter;
                return ref _next;
            }

            /// <summary>
            /// Required method for the awaitable pattern, calls <see cref="GetLock(int)"/> on the ID request.
            /// </summary>
            /// <returns>The lock</returns>
            public Lock GetResult()
                => _sync.GetLock(_id);

            /// <summary>
            /// Required method for the awaitable pattern, adds an action to <see cref="_continuation"/>
            /// </summary>
            /// <param name="continuation">The action to run after acquiring the lock.</param>
            public void OnCompleted(Action continuation)
            {
                Action c;
                do
                {
                    c = _continuation; //Get value first, will be marked as completed before the value changes, so if it's not finished, we are safe to schedule on this value.
                    if (IsCompleted)
                    {
                        continuation();
                        return;
                    }
                } while (c != Interlocked.CompareExchange(ref _continuation, c + continuation, c));
            }

            /// <summary>
            /// attempts to run the continuations, fails if we don't hold the lock.
            /// </summary>
            internal bool TryDoContinuation()
            {
                if (IsCompleted)
                {
                    Interlocked.Exchange(ref _continuation, null)?.Invoke();
                    return true;
                }
                return false;
            }

            public override string ToString()
                => $"Lock Waiter: {_id}";

            /// <summary>
            /// Inserts the waiter into the queue, starting from the specified spot.
            /// </summary>
            /// <param name="current">The current location to start attempting to insert into</param>
            /// <param name="insert">The waiter to insert into the queue</param>
            /// <returns>The waiter that we were put in front of, this is <see cref="null"/> if we're last in line</returns>
            internal static LockWaiter Insert(ref LockWaiter current, LockWaiter insert)
            {
                LockWaiter value = Volatile.Read(ref current);
                while (true)
                {
                    //Find the first field that we are less than, or is null. Fields will never get bigger, so we don't care about any fields we've visited before.
                    while (value != null && value._id - insert._id < 0)
                    {
                        current = ref value.GetNextRef(); // Get the next location, use the method to allow redirects if necessary.
                        value = Volatile.Read(ref current); // Read in the value for evaluation, since the ID is readonly
                    }

                    Volatile.Write(ref insert._next, value); // Pre-link to ensure seemless insertion

                    LockWaiter ret = Interlocked.CompareExchange(ref current, insert, value); //Attempt insertion

                    if (ret == value) //If insertion was successful, break and return our value;
                        break;

                    value = ret; // Otherwise update our value and try again.

                }

                return value; //Return the value we were inserted behind. If we are the last in the chain, this is null
            }
        }
    }
}

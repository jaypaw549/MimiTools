using MimiTools.Tools;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MimiTools.Sync
{
    public class AsyncDispatcher<T>
    {
        //The next ID a received object will have. Atomically updates when a thread or tasks requests an object.
        private int _receiver = 0;

        //The next ID a sent object will have. Atomically updates when an object is sent.
        private int _sender = 0;

        /// <summary>
        /// Unlike AsyncLock, every request will get a Dispatch object,
        /// this is the beginning where you will check first when grabbing an ID
        /// </summary>
        private Dispatch _start = null;

        /// <summary>
        /// A cache of the last dispatch value in the chain, used to speed up adding dispatches when there's long queues.
        /// Usually never up to date under high loads, but very likely to be somewhat close.
        /// </summary>
        private Dispatch _end = null;

        public int ItemsRemaining => Math.Max(_sender - _receiver, 0);
        public int WaitingReceivers => Math.Max(_receiver - _sender, 0);

        /// <summary>
        /// Enqueues the thread for receiving an object sent by <see cref="Send(T)"/>
        /// </summary>
        /// <returns>The object received</returns>
        public T Receive()
        {
            //We are consuming this, so make sure to dispose of it after.
            using Dispatch dispatch = GetOrCreateDispatch(ref _receiver);
            return dispatch.GetResult();
        }

        /// <summary>
        /// Enqueues a task for receiving an object sent by <see cref="Send(T)"/>
        /// </summary>
        /// <returns>A task that will eventually contain the object received</returns>
        public async Task<T> ReceiveAsync()
        {
            T value;
            using (Dispatch dispatch = GetOrCreateDispatch(ref _receiver))
                value = await dispatch;
            await Task.Yield();
            return value;
        }

        /// <summary>
        /// Enqueues the object for receiving by a call to <see cref="Receive"/>,
        /// <see cref="ReceiveAsync"/>, <see cref="TryReceive(out T)"/>, or <see cref="TryPeek(out T)"/>
        /// </summary>
        /// <param name="value">The object to send</param>
        public void Send(T value)
            => GetOrCreateDispatch(ref _sender).SetResult(value);

        /// <summary>
        /// Tries to peek at the first unreceived object sent by <see cref="Send(T)"/>.
        /// </summary>
        /// <param name="value">The field to write the dispatch to if successful</param>
        /// <returns>True if we were able to peek at the dispatch</returns>
        public bool TryPeek(out T value)
        {
            int id = Volatile.Read(ref _receiver);
            if (id - Volatile.Read(ref _sender) < 0)
            {
                Dispatch dispatch = Dispatch.Find(ref _start, id);
                if (dispatch != null && dispatch.IsCompleted)
                {
                    value = dispatch.GetResult();
                    return true;
                }
            }
            value = default;
            return false;
        }

        /// <summary>
        /// Tries to receive the first unreceived object sent by <see cref="Send(T)"/>.
        /// </summary>
        /// <param name="value">The field to write the dispatch to if successful</param>
        /// <returns>True if we were able to receive the dispatch</returns>
        public bool TryReceive(out T value)
        {
            int id = Volatile.Read(ref _receiver);
            if (id - Volatile.Read(ref _sender) < 0)
            {
                Dispatch dispatch = Dispatch.Find(ref _start, id);
                if (dispatch != null && dispatch.IsCompleted &&
                    id == Interlocked.CompareExchange(ref _receiver, id + 1, id))
                {
                    value = dispatch.GetResult();
                    dispatch.Dispose();
                    return true;
                }
            }
            value = default;
            return false;
        }
        
        /// <summary>
        /// Creates or fetches a dispatch of the specified ID in the dispatch queue. Lock-free thread-safe
        /// </summary>
        /// <param name="id_field">a reference to the field to fetch the ID from (will be incremented atomically)</param>
        /// <returns>a Dispatch in the queue with the ID fetched from the field</returns>
        private Dispatch GetOrCreateDispatch(ref int id_field)
        {
            Dispatch last = Volatile.Read(ref _end); //If we have to insert, this value is guaranteed to be before ours
            int id = Interlocked.Increment(ref id_field) - 1;

            ref Dispatch field = ref _start;
            if (last != null)
                field = ref last.Next;

            //Does a bunch of things, but ultimately it gets or creates our dispatch. If it returns true then we need to update our end value.
            if (Dispatch.CreateOrGet(this, id, ref field, out Dispatch dispatch))
                TryUpdateEnd(dispatch);

            return dispatch;
        }

        /// <summary>
        /// Updates the <see cref="_end"/> field to contain a value as far down or further down the queue than the specified dispatch.
        /// </summary>
        /// <param name="value">The dispatch to try to update <see cref="_end"/> to</param>
        private void TryUpdateEnd(Dispatch value)
        {
            Dispatch current = Volatile.Read(ref _end);
            while(current != null && current.Id - value.Id < 0)
            {
                Dispatch ret = Interlocked.CompareExchange(ref _end, value, current);
                if (ret == current)
                    return;

                current = ret;
            }
        }

        /// <summary>
        /// Is the queue item containing the values that were sent, or spots to send the values to.
        /// Contains Thread-safe lock-free methods for inserting into or removing from the queue.
        /// </summary>
        private class Dispatch : ICustomAwaitable<Dispatch, T>, IAwaiter<T>, IDisposable
        {
            internal Dispatch(AsyncDispatcher<T> dispatcher, int id)
            {
                Id = id;
                _completed = false;
                _delivered = false;
                _dispatcher = dispatcher;
            }

            /// <summary>
            /// The source of this dispatch item
            /// </summary>
            private readonly AsyncDispatcher<T> _dispatcher;

            /// <summary>
            /// Whether or not this dispatch has been given a value. Is set to true by <see cref="SetResult(T)"/>
            /// </summary>
            private volatile bool _completed;

            /// <summary>
            /// The continuation to run when this dispatch completes.
            /// </summary>
            private Action _continuation;

            /// <summary>
            /// Whether or not this dispatch has been delivered properly. 
            /// If true then <see cref="Next"/> returns a reference to <see cref="_start"/>
            /// instead of <see cref="_next"/>
            /// </summary>
            private volatile bool _delivered;

            /// <summary>
            /// The next dispatch in line. Gets set to ourselves when we're delivered.
            /// </summary>
            private Dispatch _next;

            /// <summary>
            /// The value in which we are delivering. Since we are generic, we have no way of knowing if this is valid by its value
            /// </summary>
            /// <seealso cref="_completed"/>
            private T _value;

            /// <summary>
            /// The ID of the dispatch. This is required to insert into the correct spot in the queue.
            /// </summary>
            public int Id { get; }

            /// <summary>
            /// A reference to the next item in line. If this item is being disposed of, then this will redirect to <see cref="_start"/>
            /// </summary>
            public ref Dispatch Next
            {
                get
                {
                    if (_delivered)
                        return ref _dispatcher._start;
                    return ref _next;
                }
            }

            /// <summary>
            /// Accesser property for <see cref="_completed"/>, also part of the awaitable pattern.
            /// </summary>
            public bool IsCompleted => _completed;

            /// <summary>
            /// Tells whether or not this item has been delivered.
            /// </summary>
            /// <seealso cref="_delivered"/>
            public bool IsDelivered => _delivered;

            /// <summary>
            /// Disposes of the Dispatch, marking it as delivered and removing it from the queue.
            /// Dispatches that get this called are near the front of the queue.
            /// </summary>
            public void Dispose()
            {
                //Start redirecting back to _start
                _delivered = true;

                //Seal the deal immediately
                Dispatch next = Interlocked.Exchange(ref _next, this);

                //Remove ourselves from the _end cache, if we need to.
                _ = Interlocked.CompareExchange(ref _dispatcher._end, null, this);

                //Remove ourselves from the queue altogether
                Replace(ref _dispatcher._start, next, this);
            }

            /// <summary>
            /// Awaitable pattern method.
            /// </summary>
            /// <returns>this</returns>
            public Dispatch GetAwaiter()
                => this;

            /// <summary>
            /// Awaitable pattern method.
            /// Waits for and fetches the object assigned to this dispatch.
            /// </summary>
            /// <returns></returns>
            public T GetResult()
            {
                SpinWait wait = new SpinWait();
                while (!IsCompleted)
                    wait.SpinOnce();
                return _value;
            }

            /// <summary>
            /// Registers or executes a method that is to be run when this dispatch is given a value.
            /// </summary>
            /// <param name="continuation"></param>
            public void OnCompleted(Action continuation)
            {
                Action c;
                do
                {
                    c = _continuation;
                    if (_completed)
                    {
                        continuation();
                        return;
                    }
                } while (c != Interlocked.CompareExchange(ref _continuation, c + continuation, c));
            }

            /// <summary>
            /// Assigns a value to this dispatch, completing it. As a result of completing it, runs any continuations
            /// that have been assigned to this dispatch.
            /// </summary>
            /// <param name="value"></param>
            internal void SetResult(T value)
            {
                _value = value; //Set value first
                Interlocked.MemoryBarrier();
                _completed = true;
                Interlocked.Exchange(ref _continuation, null)?.Invoke();
            }

            /// <summary>
            /// Three potential cases
            /// 1) We successfully inserted in the middle, we return false
            /// 2) We successfully appended to the end, we return true.
            /// 3) We already have a dispatch with our ID, we return false.
            /// </summary>
            /// <param name="dispatcher">The dispatcher that is the source of this Dispatch</param>
            /// <param name="id">The id of the dispatch we're looking to find or insert</param>
            /// <param name="current">A reference of the field to start searching from</param>
            /// <param name="value">Where to assign the dispatch we find or create</param>
            /// <returns>true if we need to update <see cref="_end"/></returns>
            internal static bool CreateOrGet(AsyncDispatcher<T> dispatcher, int id, ref Dispatch current, out Dispatch value)
            {
                //Read value initially for comparison
                Dispatch new_dispatch = null;
                Dispatch l_value = Volatile.Read(ref current);
                while (true)
                {
                    //Loop until we have our potential spot in the queue
                    while (l_value != null && l_value.Id - id < 0)
                    {
                        //Use reference property to allow us to *not* be stuck if we manage to enter a consumed Dispatch.
                        current = ref l_value.Next;

                        //Get the new value of our location
                        l_value = Volatile.Read(ref current);
                    }

                    //this ID already exists in the queue, so return it rather than creating one.
                    if (l_value != null && l_value.Id == id)
                    {
                        value = l_value;
                        return false;
                    }

                    //We might actually need a new dispatch, so create it now if we haven't already.
                    if (new_dispatch == null)
                        new_dispatch = new Dispatch(dispatcher, id);

                    //We have our potential spot, write our next to ensure seemless insertion.
                    //Write directly to the field to prevent accidentally writing to _start if our turn came up while we were working on this.
                    Volatile.Write(ref new_dispatch._next, l_value);

                    //Attempt the exchange
                    Dispatch tmp = Interlocked.CompareExchange(ref current, new_dispatch, l_value);

                    //If we were successful, return what used to be there. Could be null
                    if (tmp == l_value)
                    {
                        value = new_dispatch;
                        return l_value == null;
                    }

                    //If we weren't, update our known value and look again.
                    l_value = tmp;
                }
            }

            /// <summary>
            /// Finds a dispatch in the queue.
            /// </summary>
            /// <param name="current">The spot to start from. This is usually <see cref="_start"/></param>
            /// <param name="id">The id of the dispatch to find.</param>
            /// <returns>The dispatch that was found, or null</returns>
            internal static Dispatch Find(ref Dispatch current, int id)
            {
                Dispatch value;
                do
                {
                    value = Volatile.Read(ref current);
                    current = ref value._next;
                } while (value != null && value.Id - id < 0);

                if (value.Id == id)
                    return value;

                return null;
            }

            /// <summary>
            /// Replaces a dispatch with the specified dispatch. Basically this method is used to remove a dispatch from the queue.
            /// </summary>
            /// <param name="current">The spot to start from. This is usually <see cref="_start"/></param>
            /// <param name="value">The dispatch to put in its place, this is usually comparand's <see cref="_next"/></param>
            /// <param name="comparand">The value we're going to replace</param>
            internal static void Replace(ref Dispatch current, Dispatch value, Dispatch comparand)
            {
                while(true)
                {
                    Dispatch ret = Interlocked.CompareExchange(ref current, value, comparand);
                    if (ret == comparand)
                        return;
                    current = ref ret.Next;
                }
            }
        }
    }
}

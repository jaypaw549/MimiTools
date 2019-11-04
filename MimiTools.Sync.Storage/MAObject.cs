using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MimiTools.Sync.Storage
{
    /*
     * Basically a TSObject, except it allows creating shared accessors, which are accessors that don't prevent the creation of other shared accessors.
     * They still prevent the creation of normal accessors, and this is treatable as a TSObject<T> or TSObject. Forcefully creating a shared accessor is
     * technically possible under the current design, however it isn't implemented. Forcefully creating a normal accessor will break the access of all the
     * shared accessors. 
     * 
     * Shared accessors are actually just wrappers for a normal accessor, and their dispose function is overwritten to decrease a counter representing the
     * number of shared accessors open. The accessor that causes that counter to hit zero will dispose of the normal accessor it wraps. Whenever a shared
     * accessor is created, it increases that counter. There's some other synchronization stuff going on to make it fair, but other than that it works as described.
     */
    public class MAObject<T> : TSObject<T>
    {
        public static async Task<IAccessor[]> GetSharedAccessors(params MAObject<T>[] objects)
        {
            IAccessor[] accessors = new IAccessor[objects.Length];
            Dictionary<TSObject, IAccessor> mapping = new Dictionary<TSObject, IAccessor>();
            bool incomplete = true;

            try
            {
                while (incomplete)
                {
                    incomplete = false;
                    foreach (MAObject<T> obj in objects.Distinct())
                    {
                        if (obj == null)
                            continue;
                        if (mapping.ContainsKey(obj))
                            continue;
                        IAccessor a = await obj.CreateSharedAccessor(100).ConfigureAwait(false);
                        if (a == null)
                        {
                            foreach (Accessor access in mapping.Values)
                                access.Dispose();
                            mapping.Clear();
                            mapping[obj] = await obj.CreateSharedAccessor().ConfigureAwait(false);
                            incomplete = true;
                            break;
                        }
                        mapping[obj] = a;
                    }
                }
                for (int i = 0; i < accessors.Length; i++)
                    if (objects[i] != null)
                        accessors[i] = mapping[objects[i]];
            }
            catch (Exception)
            {
                foreach (Accessor a in mapping.Values)
                    a.Dispose();
                throw;
            }
            return accessors;
        }

        public MAObject(T value) : this(value, 0)
        {
        }

        public MAObject(TSObject<T> wrap) : this(wrap, 0)
        {
        }

        public MAObject(T value, int max_per_accessor) : base(value)
        {
            MaxSessions = max_per_accessor;
            Waiter.Pulse(true);
        }

        public MAObject(TSObject<T> wrap, int max_per_accessor) : base(wrap)
        {
            MaxSessions = max_per_accessor;
            Waiter.Pulse(true);
        }

        private volatile IAccessor Access = null;
        private volatile int ActiveCount = 0;
        private volatile int SessionCount = 0;
        private readonly int MaxSessions;
        new private readonly AsyncSync Sync = new AsyncSync();
        new private readonly AsyncWaiter Waiter = new AsyncWaiter();

        public async override Task<ITypedAccessor> CreateAccessor(CancellationToken token)
        {
            // This line is basically just us enrolling into a pseudo-queue for permission to execute, if we don't have some form of limiter attached that is, if we do, then just enroll normally
            if (MaxSessions <= 0 && !await Waiter.WaitAsync(token))
                return null;

            ITypedAccessor access = await base.CreateAccessor(token);

            if (MaxSessions <= 0)
                await Waiter.PulseAsync(true);

            return access;
        }

        public async Task<ITypedAccessor> CreateSharedAccessor(int timeout = -1)
        {
            timeout = Math.Max(-1, timeout);
            using (CancellationTokenSource source = new CancellationTokenSource(timeout))
                return await CreateSharedAccessor(source.Token);
        }

        public virtual async Task<ITypedAccessor> CreateSharedAccessor(CancellationToken token)
        {
            CancellationTokenSource source = CancellationTokenSource.CreateLinkedTokenSource(token);
            if (!await Waiter.WaitAsync(token))
                return null;


            /* Summary of the following: Enter a synchronized zone so only one task runs in here at a time
             * 1) Enters a synchronized zone so only one task is changing the private values
             * 2) Checks if we've already created an accessor, and we haven't already exceeded the creation limit
             *    2.1) If we're good, enter the synchronized zone for the superclass.
             *    2.2) Check if our accessor is still valid
             *    2.3) If it's still valid, return a new shared accessor wrapping it, if not continue
             *    2.4) Dispose of our accessor
             * 3) Leave the synchronized zone so we can wait for any leftover accessors to dispose, if still relevant
             * 4) Create an accessor to wrap
             * 5) if we didn't actually get an accessor, due to timeout or something else, return null
             * 6) Entering the synchronized zone temporarily, assign our new accessor to this object
             * 7) Return a wrapped accessor as an ITypedAccessor
             */

            Task<ITypedAccessor> access_creator = Sync.ExecuteAsync<ITypedAccessor>(async () =>
            {
                if (Access != null && SessionCount < MaxSessions && SessionCount > 0)
                {
                    if (await base.Sync.Execute(() => Access != Owner))
                        return new SharedAccessor(this);
                    else
                    {
                        Access = null;
                        ActiveCount = 0;
                    }
                }
                return null;
            }).ContinueWith(async t =>
            {
                if (t.Result != null)
                    return t.Result;

                IAccessor access = await (this as TSObject).CreateAccessor(token);

                if (access == null)
                    return null;

                await Sync.Execute(() => Access = access);

                return new SharedAccessor(this);
            }, TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.ExecuteSynchronously).Unwrap();

            /* Summary of this part
             * If we don't get our accessor in time, schedule a task to dispose of the accessor, then return null
             */

            if (await Task.WhenAny(access_creator, Task.Delay(-1, source.Token)) != access_creator)
            {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                access_creator.ContinueWith(t =>
                {
                    Waiter.Pulse(true);
                    if (!t.IsFaulted && !t.IsCanceled)
                        t.Result?.Dispose();
                }, TaskContinuationOptions.ExecuteSynchronously);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

                return null;
            }

            source.Cancel();
            await Waiter.PulseAsync(true);

            return await access_creator;
        }

        private async void OnAccessorDisposing(SharedAccessor accessor)
        {
            await Sync.ExecuteAsync(async () =>
            {
                if (accessor.Accessor == Access && await base.Sync.Execute(() => Owner == Access) && --ActiveCount == 0)
                {
                    Access.Dispose();
                    Access = null;
                    SessionCount = 0;
                }
            });
        }

        private class SharedAccessor : ITypedAccessor
        {
            internal SharedAccessor(MAObject<T> container)
            {
                Accessor = container.Access;
                container.ActiveCount++;
                container.SessionCount++;
                Container = container;
            }

            internal readonly IAccessor Accessor;
            private readonly MAObject<T> Container;
            private volatile bool Valid = true;

            T ITypedAccessor.Value { get => (T)Container.Owner.Value; set => Container.Owner.Value = value; }
            dynamic IAccessor.Value { get => Container.Owner.Value; set => Container.Owner.Value = value; }

            public void Dispose()
            {
                if (!Valid)
                    return;
                Valid = false;
                Container.OnAccessorDisposing(this);
            }

            object IAccessor.Get(TSObject obj)
            {
                if (Valid)
                    return Accessor.Get(obj);
                throw new InvalidOperationException("This accessor has been disposed of!");
            }

            V IAccessor.Get<V>(TSObject<V> obj)
            {
                if (Valid)
                    return Accessor.Get(obj);

                throw new InvalidOperationException("This accessor has been disposed of!");
            }

            void IAccessor.Set(TSObject obj, object value)
            {
                if (Valid)
                    Accessor.Set(obj, value);
                else
                    throw new InvalidOperationException("This accessor has been disposed of!");
            }

            void IAccessor.Set<V>(TSObject<V> obj, V value)
            {
                if (Valid)
                    Accessor.Set(obj, value);
                else
                    throw new InvalidOperationException("This accessor has been disposed of!");
            }
        }
    }
}

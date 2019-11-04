using MimiTools.Extensions.Tasks;
using MimiTools.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MimiTools.Sync.Storage
{
    public class TSObject
    {
        public static TSObject Create(object obj)
            => new TSObject(obj);

        /// <summary>
        /// Gets accessors for all the objects specified. This method is relatively thread-safe and null-safe. Multiple threads trying to access the same accessors using this method
        /// are unlikely to make each other deadlock. Deadlock can still occur if one thread forgets to dispose of an accessor, or hogs it to itself. Softlocking can still occur if
        /// the calls are tailored to do their best to interfere with each other, but deadlocking is impossible when using this method and accessors are disposed of properly
        /// </summary>
        /// <param name="objects">The set of objects to generate accessors for</param>
        /// <returns>An array of accessors. Duplicate and linked accessors can exist if duplicate or linked objects are in the array.</returns>
        public static async Task<IAccessor[]> GetAccessors(params TSObject[] objects)
        {
            if (objects == null)
                return null;

            IAccessor[] accessors = new IAccessor[objects.Length];
            Dictionary<TSObject, IAccessor> mapping = new Dictionary<TSObject, IAccessor>();
            HashSet<IAccessor> owned = new HashSet<IAccessor>();

            try
            {
                bool restart;
                do
                {
                    restart = false;
                    foreach (TSObject obj in objects.Distinct()) // Loop through all objects
                    {
                        if (obj == null) // We don't care if it's null, it can just be null
                            continue;

                        if (mapping.ContainsKey(obj)) // We also don't care if we've done it already
                            continue;

                        IAccessor a;

                        if (await obj.Sync.Execute(() =>
                            {
                                if (owned.Contains(obj.Owner))
                                {
                                    mapping[obj] = new ProxyAccessor(obj, obj.Owner);
                                    return true;
                                }
                                return false;
                            }))
                            continue;

                        a = await obj.CreateAccessor(100).ConfigureAwait(false); // Try to get the accessor, timeout after a 1/10th of a seccond

                        if (a == null) // If we couldn't get an accessor quickly, then release all our accessors, and wait for this one. This is to prevent deadlocks
                        {
                            foreach (IAccessor access in mapping.Values) // Dispose of all our current accessors
                                access.Dispose();
                            mapping.Clear(); // Clear them away, they're not valid anymore
                            owned.Clear(); // Clear away the owned accessors too

                            a = await obj.CreateAccessor().ConfigureAwait(false); // Wait for the accessor
                            mapping[obj] = a; // Map it
                            owned.Add(a); // Add it to the list of owned accessors

                            restart = true; // Flag the program to retry after
                            break; // End current cycle of attempts so we can have a fresh start again
                        }
                        mapping[obj] = a; // We got the accessor, so add it to our map
                        owned.Add(a); // and also add it to the list of owned accessors for linkage detection
                    }
                }
                while (restart);

                for (int i = 0; i < accessors.Length; i++) // For each accessor requested, if its not null, assign the accessor we got to it.
                    if (objects[i] != null)
                        accessors[i] = mapping[objects[i]];
            }
            catch
            {
                foreach (Accessor a in mapping.Values) // Dispose of all the accessors, so we don't leave them in an unusable state
                    a.Dispose();
                throw;
            }
            return accessors; // Return our array of accessors. If there were duplicate objects, then there will be duplicate accessors, same with null objects and null accessors
        }

        protected virtual bool Broken { get; set; } = false;
        protected virtual Accessor Owner { get; set; } = null;
        protected virtual PriorityAsyncSync Sync { get; } = new PriorityAsyncSync();
        protected virtual AsyncWaiter Waiter { get; set; } = new AsyncWaiter();
        protected virtual dynamic Value { get => _value; set => _value = value; }

        private volatile dynamic _value;

        protected TSObject(object value)
        {
            _value = value;
            Waiter?.Pulse(true);
        }

        public async Task<IAccessor> CreateAccessor(int timeout = -1)
        {
            timeout = Math.Max(timeout, -1);
            using (CancellationTokenSource source = new CancellationTokenSource(timeout))
                return await CreateAccessor(source.Token);
        }

        public virtual async Task<IAccessor> CreateAccessor(CancellationToken token)
        {
            CancellationTokenSource source = CancellationTokenSource.CreateLinkedTokenSource(token);
            Task timeout_t = Task.Delay(-1, source.Token);
            if (!await Waiter.WaitAsync(token))
                return null;

            Task<Accessor> access_creator = Sync.ExecuteAsync(async () =>
            {
                // Quick check to reduce chance of making a useless object
                if (timeout_t.IsCompleted)
                    return null;

                if (Broken)
                {
                    await Waiter.PulseAsync(true);
                    throw new InvalidOperationException("This object's access has been broken into!");
                }

                if (Owner == null)
                    return new Accessor(this);

                return null;
            });

            if (await Task.WhenAny(access_creator, timeout_t) == timeout_t)
            {
#pragma warning disable CS4014

                access_creator.ContinueWith(t => t.Result?.Dispose(), TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnRanToCompletion);
                return null;

#pragma warning restore CS4014
            }

            source.Cancel();

            return await access_creator;
        }

        public virtual async Task<IAccessor> ForceCreateAccessor()
            => await Sync.PriorityExecute(() =>
            {
                if (Broken)
                    throw new InvalidOperationException("This object already has a forced accessor!");
                Owner = null;
                return new ForcedAccessor(this);
            });

        public virtual TSObject<T> CastOrConvert<T>()
            => TSObject<T>.Wrap(this);

        public TSObject CreateLinked(object obj)
            => new TSObjectLinker(this, obj);

        public TSObject<T> CreateLinked<T>(T obj)
            => TSObject<T>.Wrap(new TSObjectLinker(this, obj));

        public interface IAccessor : IDisposable
        {
            dynamic Value { get; set; }

            object Get(TSObject obj);

            V Get<V>(TSObject<V> obj);

            void Set(TSObject obj, object value);

            void Set<V>(TSObject<V> obj, V value);
        }

        protected class Accessor : IDisposable, IAccessor
        {
            protected readonly TSObject Container;

            protected internal Accessor(TSObject container)
            {
                container.Owner = container.Owner ?? this;

                if (container.Owner != this)
                    throw new InvalidOperationException("Cannot claim ownership of this object!");

                Container = container;
            }

            public dynamic Value
            {
                get
                {
                    if (DisposedValue)
                        throw new ObjectDisposedException("Accessor");
                    return Container.Sync.PriorityExecute(() =>
                    {
                        if (Container.Owner != this)
                            throw new InvalidOperationException("This access to this object has been revoked!");
                        return Container.Value;
                    }).WaitAndUnwrapException();
                }

                set
                {
                    if (DisposedValue)
                        throw new ObjectDisposedException("Accessor");

                    Container.Sync.PriorityExecute(() =>
                    {
                        if (Container.Owner != this)
                            throw new InvalidOperationException("This access to this object has been revoked!");
                        Container.Value = value;
                    }).WaitAndUnwrapException();
                }
            }

            public object Get(TSObject obj)
            {
                if (!Container.Sync.PriorityExecute(() =>
                {
                    if (obj.Owner != this)
                        return false;
                    return true;
                }).WaitAndUnwrapException())
                    throw new InvalidOperationException("This object isn't owned by this accessor!");

                return obj.Value;
            }

            public void Set(TSObject obj, object value)
            {
                if (!Container.Sync.PriorityExecute(() =>
                {
                    if (obj.Owner != this)
                        return false;

                    obj.Value = value;

                    return true;
                }).WaitAndUnwrapException())
                    throw new InvalidOperationException("This object isn't owned by this accessor!");
            }

            public V Get<V>(TSObject<V> obj)
                => (V)Get((TSObject)obj);

            public void Set<V>(TSObject<V> obj, V value)
                => Set(obj, (object)value);

            #region IDisposable Support
            protected bool DisposedValue { get; set; } = false; // To detect redundant calls

            protected virtual void Dispose(bool disposing)
            {
                if (!DisposedValue)
                {
                    if (disposing)
                    {
                        Container.Sync.PriorityExecute(() =>
                        {
                            if (Container.Owner == this)
                            {
                                Container.Owner = null;
                                Container.Waiter.Pulse(true);
                            }
                        }).WaitAndUnwrapException();

                    }

                    // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                    // TODO: set large fields to null.

                    DisposedValue = true;
                }
            }

            // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
            // ~Accessor() {
            //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            //   Dispose(false);
            // }

            // This code added to correctly implement the disposable pattern.
            public virtual void Dispose()
            {
                // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
                Dispose(true);
                // TODO: uncomment the following line if the finalizer is overridden above.
                // GC.SuppressFinalize(this);
            }
            #endregion

        }

        protected class ForcedAccessor : Accessor
        {
            protected internal ForcedAccessor(TSObject container) : base(container)
            {
                container.Broken = true;
                Container.Waiter.PulseAll();
            }

            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);
                if (disposing)
                    Container.Sync.PriorityExecute(() => Container.Broken = false).WaitAndUnwrapException();
            }
        }

        protected class ProxyAccessor : IAccessor
        {
            internal ProxyAccessor(TSObject source, IAccessor access)
            {
                Source = source;
                Access = access;
            }

            private readonly TSObject Source;
            private readonly IAccessor Access;

            public dynamic Value { get => Access.Get(Source); set => Access.Set(Source, value); }

            public void Dispose()
                => Access.Dispose();

            public object Get(TSObject obj)
                => Access.Get(obj);

            public V Get<V>(TSObject<V> obj)
                => Access.Get(obj);

            public void Set(TSObject obj, object value)
                => Access.Set(obj, value);

            public void Set<V>(TSObject<V> obj, V value)
                => Access.Set(obj, value);
        }

        private class TSObjectLinker : TSObject
        {
            private readonly TSObject Linked;

            internal TSObjectLinker(TSObject link, object value) : base(value)
            {
                Linked = link;
                Waiter.Pulse(true);
            }

            protected override Accessor Owner { get => Linked.Owner; set => Linked.Owner = value; }
            protected override AsyncWaiter Waiter { get => Linked?.Waiter; set => Linked.Waiter = value; }
        }

        protected class TSObjectWrapper : TSObject
        {
            internal TSObjectWrapper(TSObject obj) : base(obj)
            { }

            internal bool BrokenWrapper { get => (Value as TSObject).Broken; set => (Value as TSObject).Broken = value; }
            internal AsyncWaiter NotifierWrapper { get => (Value as TSObject).Waiter; set => (Value as TSObject).Waiter = value; }
            internal Accessor OwnerWrapper { get => (Value as TSObject).Owner; set => (Value as TSObject).Owner = value; }
            internal PriorityAsyncSync SyncWrapper { get => (Value as TSObject).Sync; }
            internal dynamic ValueWrapper { get => (Value as TSObject).Value; set => (Value as TSObject).Value = value; }
        }
    }

    public class TSObject<T> : TSObject
    {
        public static async Task<ITypedAccessor[]> GetAccessors(params TSObject<T>[] objects)
        {
            if (objects == null)
                return null;

            ITypedAccessor[] accessors = new ITypedAccessor[objects.Length];
            IAccessor[] base_accessors = await TSObject.GetAccessors(objects);

            for (int i = 0; i < accessors.Length; i++)
                if (base_accessors[i] != null)
                    accessors[i] = new TypeWrappedAccessor(base_accessors[i]);
                else
                    accessors[i] = null;

            return accessors;
        }

        public static TSObject<T> Wrap(TSObject obj)
            => new TSObject<T>(obj);

        private readonly bool Wrapper;

        protected override bool Broken
        {
            get
            {
                if (Wrapper)
                    return (base.Value as TSObjectWrapper).BrokenWrapper;
                return base.Broken;
            }
            set
            {
                if (Wrapper)
                    (base.Value as TSObjectWrapper).BrokenWrapper = value;
                else
                    base.Broken = value;
            }
        }

        protected override PriorityAsyncSync Sync
        {
            get
            {
                if (Wrapper)
                    return (base.Value as TSObjectWrapper).SyncWrapper;
                return base.Sync;
            }
        }

        protected override AsyncWaiter Waiter
        {
            get
            {
                if (Wrapper)
                    return (base.Value as TSObjectWrapper).NotifierWrapper;
                return base.Waiter;
            }
            set
            {
                if (Wrapper)
                    (base.Value as TSObjectWrapper).NotifierWrapper = value;
                else
                    base.Waiter = value;
            }
        }

        protected override Accessor Owner
        {
            get
            {
                if (Wrapper)
                    return (base.Value as TSObjectWrapper).OwnerWrapper;
                return base.Owner;
            }
            set
            {
                if (Wrapper)
                    (base.Value as TSObjectWrapper).OwnerWrapper = value;
                else
                    base.Owner = value;
            }
        }

        protected override dynamic Value
        {
            get
            {
                if (Wrapper)
                    return (base.Value as TSObjectWrapper).ValueWrapper;
                return base.Value;
            }
            set
            {
                if (Wrapper)
                    (base.Value as TSObjectWrapper).ValueWrapper = value;
                base.Value = value;
            }
        }

        public TSObject() : base(default(T))
        {
            Wrapper = false;
        }

        public TSObject(T value) : base(value)
        {
            Wrapper = false;
        }

        protected TSObject(TSObject wrap) : this(new TSObjectWrapper(wrap))
        {
        }

        private TSObject(TSObjectWrapper wrapper) : base(wrapper)
        {
            Wrapper = true;
        }

        public override TSObject<V> CastOrConvert<V>()
        {
            if (this is TSObject<V>) // Same type casting
                return this as TSObject<V>;

            Type t = typeof(T);
            Type v = typeof(V);

            if (t.IsSubclassOf(v)) // If we are casting to a superclass, "Select" the variable casted to the supertype so we don't have to worry about inappropriate variable setting
                return Select(CastingTools.CreateCastingDelegate<T, V>());

            if (v.IsSubclassOf(t)) // If we are casting to a subclass, just create a wrapped object of the subtype
                return new TSObject<V>(this);

            //We aren't casting, so convert then
            return Sync.PriorityExecute(() => new TSObject<V>((V)Value)).WaitAndUnwrapException();
        }

        new public async Task<ITypedAccessor> CreateAccessor(int timeout = -1)
        {
            timeout = Math.Max(-1, timeout);
            using (CancellationTokenSource source = new CancellationTokenSource(timeout))
                return await CreateAccessor(source.Token);
        }

        new public virtual async Task<ITypedAccessor> CreateAccessor(CancellationToken token)
        {
            IAccessor access = new TypeWrappedAccessor(await base.CreateAccessor(token));
            if (access == null)
                return null;
            return new TypeWrappedAccessor(access);
        }

        new public virtual async Task<ITypedAccessor> ForceCreateAccessor()
            => new TypeWrappedAccessor(await base.ForceCreateAccessor());

        public TSObject<V> Select<V>(Func<T, V> selector)
            => new SelectedTSObject<V>(this, selector);

        public interface ITypedAccessor : IAccessor
        {
            new T Value { get; set; }
        }

        protected class TypeWrappedAccessor : ITypedAccessor
        {
            private readonly IAccessor Wrapped;

            protected internal TypeWrappedAccessor(IAccessor wrapped)
            {
                Wrapped = wrapped;
            }

            T ITypedAccessor.Value { get => (T)Wrapped.Value; set => Wrapped.Value = value; }

            dynamic IAccessor.Value { get => Wrapped.Value; set => Wrapped.Value = value; }

            void IDisposable.Dispose()
                => Wrapped.Dispose();

            object IAccessor.Get(TSObject obj)
                => Wrapped.Get(obj);

            V IAccessor.Get<V>(TSObject<V> obj)
                => Wrapped.Get(obj);

            void IAccessor.Set(TSObject obj, object value)
                => Wrapped.Set(obj, value);

            void IAccessor.Set<V>(TSObject<V> obj, V value)
                => Wrapped.Set(obj, value);
        }

        private class SelectedTSObject<V> : TSObject<V>
        {
            private readonly Func<T, V> Selector;
            private readonly string Error;
            internal SelectedTSObject(TSObject<T> obj, Func<T, V> selector, string error = "Cannot set through a selected object!") : base(obj)
            {
                Selector = selector;
                Error = error;
            }

            protected override dynamic Value { get => Selector((T)base.Value); set => throw new NotSupportedException(Error); }
        }
    }
}

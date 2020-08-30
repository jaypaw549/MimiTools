using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using MimiTools.Sync;

namespace MimiTools.Sync.Storage
{
    public class RWObject<T> : TSObject<T>
    {
        public RWObject(T value) : base(value, new AdvancedLock())
        {
        }

        private IAsyncShareableLockable m_shared_lock => m_access_lock as IAsyncShareableLockable;

        public virtual ITypedAccessor<T> CreateSharedAccessor(CancellationToken token = default, long timeout = -1)
            => new SharedTypedAccessor(this, m_shared_lock.GetSharedLock(token, timeout));

        public virtual async Task<ITypedAccessor<T>> CreateSharedAccessorAsync(CancellationToken token = default, long timeout = -1)
            => new SharedTypedAccessor(this, await m_shared_lock.GetSharedLockAsync(token, timeout));

        protected class SharedTypedAccessor : ITypedAccessor<T>
        {
            private readonly ILock access;
            private readonly RWObject<T> source;

            internal SharedTypedAccessor(RWObject<T> source, ILock l)
            {
                access = l;
                this.source = source;
            }

            public ref T Field => throw new InvalidOperationException();

            public RWObject<T> Target => source;

            public T Value
            {
                get => access.IsValid ? source.m_value : throw new InvalidOperationException();
                set => throw new InvalidOperationException();
            }

            public Type Type => typeof(T);

            IThreadSafeAccessible IAccessor.Target => Target;

            IThreadSafeAccessible<T> ITypedAccessor<T>.Target => Target;

            object IAccessor.Value { get => Value; set => throw new InvalidOperationException(); }

            ref readonly T ITypedAccessor<T>.ReadOnlyField
            {
                get
                {
                    if (access.IsValid)
                        return ref source.m_value;
                    throw new InvalidOperationException();
                }
            }

            bool IAccessor.IsReadOnly => true;

            public void Dispose()
                => access.Dispose();
        }
    }
}

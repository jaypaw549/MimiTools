using MimiTools.Extensions.Tasks;
using MimiTools.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MimiTools.Sync.Storage
{
    public class TSObject<T> : IThreadSafeAccessible<T>
    {
        public TSObject(T value) : this(value, new ReentrantLock())
        {
        }

        protected TSObject(T value, IAsyncLockable access_lock)
        {
            m_access_lock = access_lock;
            m_value = value;
        }

        protected T m_value;
        protected readonly IAsyncLockable m_access_lock;

        public virtual ITypedAccessor<T> CreateAccessor(CancellationToken token = default, long timeout = -1)
            => new ExclusiveTypedAccessor(this, m_access_lock.GetLock(token, timeout));

        public virtual async Task<ITypedAccessor<T>> CreateAccessorAsync(CancellationToken token = default, long timeout = -1)
            => new ExclusiveTypedAccessor(this, await m_access_lock.GetLockAsync(token, timeout));

        IAccessor IThreadSafeAccessible.CreateAccessor(CancellationToken token, long timeout)
            => CreateAccessor(token, timeout);

        async Task<IAccessor> IThreadSafeAccessible.CreateAccessorAsync(CancellationToken token, long timeout)
            => await CreateAccessorAsync(token, timeout);

        protected class ExclusiveTypedAccessor : ITypedAccessor<T>
        {
            private readonly ILock access;
            private readonly TSObject<T> source;

            internal ExclusiveTypedAccessor(TSObject<T> source, ILock l)
            {
                access = l;
                this.source = source;
            }

            public ref T Field
            {
                get
                {
                    if (access.IsValid)
                        return ref source.m_value;
                    else 
                        throw new InvalidOperationException();
                }
            }

            public TSObject<T> Target => source;

            public T Value
            {
                get => access.IsValid ? source.m_value : throw new InvalidOperationException();
                set 
                { 
                    if (access.IsValid) 
                        source.m_value = value; 
                    else 
                        throw new NotImplementedException();
                }
            }

            public Type Type => typeof(T);

            IThreadSafeAccessible IAccessor.Target => Target;

            IThreadSafeAccessible<T> ITypedAccessor<T>.Target => Target;

            object IAccessor.Value { get => Value; set => Value = (T) value; }

            ref readonly T ITypedAccessor<T>.ReadOnlyField => ref Field;

            bool IAccessor.IsReadOnly => false;

            public void Dispose()
                => access.Dispose();
        }
    }
}

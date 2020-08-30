using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MimiTools.Sync.Storage
{
    public interface IThreadSafeAccessible
    {
        IAccessor CreateAccessor(CancellationToken token = default, long timeout = Timeout.Infinite);

        Task<IAccessor> CreateAccessorAsync(CancellationToken token = default, long timeout = Timeout.Infinite);
    }

    public interface IThreadSafeAccessible<T> : IThreadSafeAccessible
    {
        new ITypedAccessor<T> CreateAccessor(CancellationToken token = default, long timeout = Timeout.Infinite);

        new Task<ITypedAccessor<T>> CreateAccessorAsync(CancellationToken token = default, long timeout = Timeout.Infinite);
    }
}

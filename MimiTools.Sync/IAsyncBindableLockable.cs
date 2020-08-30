using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MimiTools.Sync
{
    public interface IAsyncBindableLockable : IBindableLockable, IAsyncLockable
    {
        new Task<IBindableLock> GetLockAsync();

        new IBindableLockRequest RequestLock();
    }
}

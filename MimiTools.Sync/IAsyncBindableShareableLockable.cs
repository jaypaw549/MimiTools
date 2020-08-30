using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MimiTools.Sync
{
    public interface IAsyncBindableShareableLockable : IAsyncBindableLockable, IAsyncShareableLockable
    {
        new IBindableLock GetSharedLock();

        new Task<IBindableLock> GetSharedLockAsync();

        new IBindableLockRequest RequestSharedLock();
    }
}

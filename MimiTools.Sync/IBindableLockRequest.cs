using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MimiTools.Sync
{
    public interface IBindableLockRequest : ILockRequest
    {
        new IBindableLock GetLock();
        new Task<IBindableLock> GetLockAsync();
    }
}

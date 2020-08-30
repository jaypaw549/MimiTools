using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MimiTools.Sync
{
    public interface IAsyncLockable : ILockable
    {
        Task<ILock> GetLockAsync();

        ILockRequest RequestLock();
    }
}

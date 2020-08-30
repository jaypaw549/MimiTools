using System;
using System.Collections.Generic;
using System.Text;

namespace MimiTools.Sync
{
    public interface IShareableLockable : ILockable
    {
        ILock GetSharedLock();
    }
}

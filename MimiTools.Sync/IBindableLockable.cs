using System;
using System.Collections.Generic;
using System.Text;

namespace MimiTools.Sync
{
    public interface IBindableLockable : ILockable
    {
        new IBindableLock GetLock();
    }
}

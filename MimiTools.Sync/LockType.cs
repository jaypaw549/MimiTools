using System;
using System.Collections.Generic;
using System.Text;

namespace MimiTools.Sync
{
    [Flags]
    public enum LockType
    {
        SHARED = 0,
        EXCLUSIVE = 1,
        UPGRADEABLE = 2,
    }
}

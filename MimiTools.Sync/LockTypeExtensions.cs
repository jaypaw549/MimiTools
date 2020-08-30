using System;
using System.Collections.Generic;
using System.Text;

namespace MimiTools.Sync
{
    public static class LockTypeExtensions
    {
        public static bool IsExclusive(this LockType type)
            => (type & LockType.EXCLUSIVE) == LockType.EXCLUSIVE;

        public static bool IsUpgradeable(this LockType type)
            => (type & LockType.UPGRADEABLE) == LockType.UPGRADEABLE;
    }
}

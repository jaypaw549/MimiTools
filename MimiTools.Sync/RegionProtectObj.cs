using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace MimiTools.Sync
{
    public class RegionProtectObj : IShareableLockable
    {
        private RegionProtect64 m_protect = new RegionProtect64();

        public SharedRegion EnterSharedRegion()
        {
            m_protect.EnterSharedRegion();
            return new SharedRegion(this);
        }

        public ExclusiveRegion EnterExclusiveRegion()
        {
            m_protect.EnterExclusiveRegion();
            return new ExclusiveRegion(this);
        }

        private void ExitRegion(bool exclusive)
            => m_protect.ExitRegion(exclusive);

        ILock IShareableLockable.GetSharedLock()
            => EnterSharedRegion();

        ILock ILockable.GetLock()
            => EnterExclusiveRegion();

        public struct SharedRegion : ILock
        {
            internal SharedRegion(RegionProtectObj obj)
            {
                m_protect = obj;
            }

            private RegionProtectObj m_protect;

            public bool IsValid => m_protect != null;

            public void Dispose()
                => Interlocked.Exchange(ref m_protect, null)?.ExitRegion(false);

            void ILock.Release()
                => Dispose();
        }

        public struct ExclusiveRegion : ILock
        {
            internal ExclusiveRegion(RegionProtectObj obj)
            {
                m_protect = obj;
            }

            private RegionProtectObj m_protect;

            public bool IsValid => m_protect != null;

            public void Dispose()
                => Interlocked.Exchange(ref m_protect, null)?.ExitRegion(true);

            void ILock.Release()
                => Dispose();
        }
    }
}

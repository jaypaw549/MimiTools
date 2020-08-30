using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Transactions;

namespace MimiTools.Sync
{
    public sealed partial class AdvancedLock
    {
        private class ObjectAccess
        {
            internal ObjectAccess(AdvancedLock l, ObjectAccess parent, LockObject obj)
            {
                m_lock = l;
                m_parent = parent;
                m_obj = obj;
                m_protect = new RegionProtect64();
            }

            private readonly AdvancedLock m_lock;
            private readonly ObjectAccess m_parent;
            private volatile ILockObject m_obj;
            private RegionProtect64 m_protect;

            internal ObjectAccess Parent => m_parent;

            internal void Bind()
                => m_lock.Bind(this);

            internal bool Cancel()
            {
                bool ret;
                RegionProtect64.Region region = default;
                try
                {
                    ILockObject obj = GetObjectSafe(true, out region);
                    ret = obj.Cancel();

                    if (ret && obj is LockObject target)
                    {
                        m_obj = target.GetObjectState();
                        Recycle(target);
                    }
                }
                finally
                {
                    m_protect.ExitRegion(ref region);
                }
                return ret;
            }

            internal bool CheckValue(StateValues value, StateValues mask)
            {
                RegionProtect64.Region region = default;
                try
                {
                    return GetObjectSafe(false, out region).CheckValue(value, mask);
                }
                finally
                {
                    m_protect.ExitRegion(ref region);
                }
            }

            internal void Downgrade()
            {
                RegionProtect64.Region region = default;
                try
                {
                    GetObjectSafe(false, out region).Downgrade();
                }
                finally
                {
                    m_protect.ExitRegion(ref region);
                }
            }

            internal void ExitBindRegion(ref RegionProtect64.Region region)
                => m_protect.ExitRegion(ref region);

            private ILockObject GetObjectSafe(bool exclusive, out RegionProtect64.Region region)
            {
                region = m_protect.EnterRegion(exclusive);
                return m_obj;
            }

            internal static LockObject GetBindTarget(ref ObjectAccess access, out RegionProtect64.Region region)
            {
                bool success = false;
                region = default;
                try
                {
                    ILockObject obj = access?.GetObjectSafe(false, out region);
                    while (obj != null && !obj.CheckValue(StateValues.GRANTED, StateValues.STATE_MASK))
                    {
                        access.m_protect.ExitRegion(ref region);
                        access = access.m_parent;
                        obj = access?.GetObjectSafe(false, out region);
                    }
                    success = true;
                    return obj as LockObject;
                }
                finally
                {
                    if (!success)
                        access.m_protect.ExitRegion(ref region);
                }
            }

            internal void OnCancelledOrGranted(Action continuation)
            {
                bool execute;
                RegionProtect64.Region region = default;
                try
                {
                    execute = !GetObjectSafe(false, out region).OnCancelledOrGranted(continuation);
                }
                finally
                {
                    m_protect.ExitRegion(ref region);
                }

                if (execute)
                    continuation();
            }

            internal void OnUpgraded(Action continuation)
            {
                bool execute;
                RegionProtect64.Region region = default;
                try
                {
                    execute = !GetObjectSafe(false, out region).OnUpgraded(continuation);
                }
                finally
                {
                    m_protect.ExitRegion(ref region);
                }
                if (execute)
                    continuation();
            }

            internal void Release()
            {
                RegionProtect64.Region region = default;
                try
                {
                    ILockObject obj = GetObjectSafe(true, out region);
                    obj.Release();

                    if (obj is LockObject target && target.CheckValue(StateValues.RELEASED, StateValues.STATE_MASK))
                    {
                        m_obj = target.GetObjectState();
                        Recycle(target);
                    }
                }
                finally
                {
                    m_protect.ExitRegion(ref region);
                }
            }

            internal static bool TargetEquals(ObjectAccess x, ObjectAccess y)
            {
                RegionProtect64.Region region = default;
                try
                {
                    while(x != null)
                    {
                        if (x.GetObjectSafe(false, out region) is LockObject)
                            break;

                        x.m_protect.ExitRegion(ref region);
                        x = x.Parent;
                    }

                    while (x != y && y != null)
                        y = y.Parent;

                    return x == y;
                }
                finally
                {
                    if (region.IsActive)
                        x.m_protect.ExitRegion(ref region);
                }
                
            }

            internal void Unbind()
                => m_lock.Unbind(this);

            internal void Upgrade()
            {
                RegionProtect64.Region region = default;
                try
                {
                    GetObjectSafe(false, out region).Upgrade();
                }
                finally
                {
                    m_protect.ExitRegion(ref region);
                }
            }
        }
    }
}
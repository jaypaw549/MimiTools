using MimiTools.Memory;
using MimiTools.Tools;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MimiTools.Sync
{
    /// <summary>
    /// A synchronization primitive meant to replace asynchronous locking. It is "Advanced" because maximum control is given to the user on how to manage waiting for the lock.
    /// It also supports asynchronous reentry.
    /// </summary>
    public sealed partial class AdvancedLock : IAsyncBindableShareableLockable
    {
        private static ObjectPoolStruct<LockObject> MEMORY_POOL = new ObjectPoolStruct<LockObject>(20);

        /// <summary>
        /// Used for recursive binding, tracks the currently acquired lock for a logical thread.
        /// </summary>
        private readonly AsyncLocal<ObjectAccess> m_bind = new AsyncLocal<ObjectAccess>();

        /// <summary>
        /// Root of our queue tree thingy, will always be granted and root. All valid requests are children of this.
        /// </summary>
        private readonly LockObject m_root;

        public AdvancedLock()
        {
            m_root = NewObject();
            m_root.SetExclusive();
            m_root.GrantRoot();
        }

        private void Bind(ObjectAccess bind)
        {
            if (bind == m_bind.Value)
                return;

            if (!ObjectAccess.TargetEquals(bind.Parent, m_bind.Value))
                throw new InvalidOperationException("You haven't bound the parent!");

            m_bind.Value = bind;
        }

        private static LockObject NewObject()
        {
            if (!MEMORY_POOL.TryRemove(out LockObject obj))
                obj = new LockObject();

            return obj;
        }

        private static void Recycle(LockObject obj)
        {
            if (MEMORY_POOL.TryAdd(obj))
                return;
            obj.Dispose();
        }

        public LockRequest RequestLock(LockType type)
        {
            LockObject obj = NewObject();

            if (type.IsExclusive())
                obj.SetExclusive();

            if (type.IsUpgradeable())
                obj.SetUpgradeable();

            RegionProtect64.Region region = default;
            ObjectAccess parent_access = m_bind.Value;
            LockObject parent_obj;
            try
            {
                while(true)
                {
                    parent_obj = ObjectAccess.GetBindTarget(ref parent_access, out region) ?? m_root;
                    if (parent_obj.AppendChild(obj))
                        break;

                    parent_access.ExitBindRegion(ref region);
                    parent_access = parent_access.Parent;
                }

                obj.Grant();

                return new LockRequest(new ObjectAccess(this, parent_access, obj));
            }
            finally
            {
                parent_access?.ExitBindRegion(ref region);
            }
        }

        public void Unbind()
            => m_bind.Value = null;

        private void Unbind(ObjectAccess current)
        {
            if (current == m_bind.Value)
                m_bind.Value = current.Parent;
        }

        ILock ILockable.GetLock()
            => RequestLock(LockType.EXCLUSIVE).GetLock();

        IBindableLock IBindableLockable.GetLock()
            => RequestLock(LockType.EXCLUSIVE).GetLock();

        async Task<ILock> IAsyncLockable.GetLockAsync()
            => await RequestLock(LockType.EXCLUSIVE);

        async Task<IBindableLock> IAsyncBindableLockable.GetLockAsync()
            => await RequestLock(LockType.EXCLUSIVE);

        ILock IShareableLockable.GetSharedLock()
            => RequestLock(LockType.SHARED).GetLock();

        IBindableLock IAsyncBindableShareableLockable.GetSharedLock()
            => RequestLock(LockType.SHARED).GetLock();

        async Task<IBindableLock> IAsyncBindableShareableLockable.GetSharedLockAsync()
            => await RequestLock(LockType.SHARED);

        async Task<ILock> IAsyncShareableLockable.GetSharedLockAsync()
            => await RequestLock(LockType.SHARED);

        IBindableLockRequest IAsyncBindableLockable.RequestLock()
            => RequestLock(LockType.EXCLUSIVE);

        ILockRequest IAsyncLockable.RequestLock()
            => RequestLock(LockType.EXCLUSIVE);

        IBindableLockRequest IAsyncBindableShareableLockable.RequestSharedLock()
            => RequestLock(LockType.SHARED);

        ILockRequest IAsyncShareableLockable.RequestSharedLock()
            => RequestLock(LockType.SHARED);
    }
}
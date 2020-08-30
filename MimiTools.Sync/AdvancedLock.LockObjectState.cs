using System;
using System.Threading;

namespace MimiTools.Sync
{
    public partial class AdvancedLock
    {
        private class LockObjectState : ILockObject
        {
            internal LockObjectState(StateValues state)
            {
                m_state = state;
            }

            private readonly StateValues m_state;

            public bool Cancel()
                => CheckValue(StateValues.CANCELLED, StateValues.STATE_MASK);

            public void CancelOrThrow()
            {
                if (!Cancel())
                    throw new InvalidOperationException();
            }

            public bool CheckValue(StateValues value, StateValues mask)
                => (m_state & mask) == value;

            public void Downgrade()
                => throw new InvalidOperationException();

            public bool OnCancelledOrGranted(Action continuation)
                => false;

            public bool OnUpgraded(Action continuation)
                => false;

            public void Release()
            {
                if (!CheckValue(StateValues.RELEASED, StateValues.STATE_MASK))
                    throw new InvalidOperationException();
            }

            public void Upgrade()
                => throw new InvalidOperationException();
        }
    }
}

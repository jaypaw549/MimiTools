using System;

namespace MimiTools.Sync
{
    public partial class AdvancedLock
    {
        private interface ILockObject
        {
            bool Cancel();
            void CancelOrThrow();
            bool CheckValue(StateValues value, StateValues mask);
            void Downgrade();
            bool OnCancelledOrGranted(Action continuation);
            bool OnUpgraded(Action continuation);
            void Release();
            void Upgrade();
        }
    }
}

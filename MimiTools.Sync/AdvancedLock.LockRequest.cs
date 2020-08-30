using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MimiTools.Sync
{
    public partial class AdvancedLock
    {
        public readonly struct LockRequest :  IBindableLockRequest
        {
            private readonly ObjectAccess m_obj;

            internal LockRequest(object obj)
            {
                m_obj = obj as ObjectAccess ?? throw new ArgumentException(nameof(obj));
            }

            public bool IsCanceled => m_obj.CheckValue(StateValues.CANCELLED, StateValues.STATE_MASK);

            public bool IsExclusive => m_obj.CheckValue(StateValues.EXCLUSIVE, StateValues.EXCLUSIVE);

            public bool IsPending => m_obj.CheckValue(StateValues.UNSET, StateValues.STATE_MASK);

            public bool IsGranted => m_obj.CheckValue(StateValues.GRANTED, StateValues.STATE_MASK);

            public bool IsReleased => m_obj.CheckValue(StateValues.RELEASED, StateValues.STATE_MASK);

            public bool IsUpgradeable => m_obj.CheckValue(StateValues.UPGRADEABLE, StateValues.UPGRADEABLE);

            bool ILockRequest.IsCompleted => !IsPending;

            public bool Cancel()
                => m_obj.Cancel();

            public Lock GetLock()
            {
                SpinWait wait = new SpinWait();

                while (IsPending)
                    wait.SpinOnce();

                if (IsGranted)
                    return new Lock(m_obj);
                
                if (IsCanceled)
                    throw new OperationCanceledException();

                if (IsReleased)
                    throw new InvalidOperationException("This request has already been released!");

                throw new Exception("I don't know what's going on!!!");
            }

            public void OnCancelledOrGranted(Action continuation)
                => m_obj.OnCancelledOrGranted(continuation);

            public void Wait()
            {
                SpinWait wait = new SpinWait();

                while (IsPending)
                    wait.SpinOnce();
            }

            IBindableLock IBindableLockRequest.GetLock()
                => GetLock();

            ILock ILockRequest.GetLock()
                => GetLock();

            async Task<IBindableLock> IBindableLockRequest.GetLockAsync()
                => await this;

            async Task<ILock> ILockRequest.GetLockAsync()
                => await this;

            void ILockRequest.OnCompleted(Action continuation)
                => OnCancelledOrGranted(continuation);

            async Task ILockRequest.WaitAsync()
                => await this.WaitAsync();
        }
    }
}

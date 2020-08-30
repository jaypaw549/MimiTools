using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace MimiTools.Sync
{
    public partial class AdvancedLock
    {
        public readonly struct UpgradeRequest
        {
            private readonly ObjectAccess m_obj;

            public bool IsPending => m_obj.CheckValue(StateValues.UNSET, StateValues.EXCLUSIVE);

            public bool IsUpgraded => m_obj.CheckValue(StateValues.EXCLUSIVE, StateValues.EXCLUSIVE);

            internal UpgradeRequest(object obj)
            {
                m_obj = obj as ObjectAccess ?? throw new ArgumentException(nameof(obj));
            }

            public void OnCompleted(Action continuation)
                => m_obj.OnUpgraded(continuation);

            public void Wait()
            {
                SpinWait spin = new SpinWait();
                while (IsPending)
                    spin.SpinOnce();
            }
        }
    }
}

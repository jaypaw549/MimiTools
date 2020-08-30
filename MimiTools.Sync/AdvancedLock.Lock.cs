using System;
using System.Collections.Generic;
using System.Text;

namespace MimiTools.Sync
{
    public partial class AdvancedLock
    {
        public readonly struct Lock : IBindableLock
        {
            private readonly ObjectAccess m_obj;

            public bool IsExclusive => m_obj.CheckValue(StateValues.EXCLUSIVE, StateValues.EXCLUSIVE);

            public bool IsUpgradeable => m_obj.CheckValue(StateValues.UPGRADEABLE, StateValues.UPGRADEABLE);

            public bool IsValid => m_obj.CheckValue(StateValues.GRANTED, StateValues.STATE_MASK);

            internal Lock(object obj)
            {
                m_obj = obj as ObjectAccess ?? throw new ArgumentException(nameof(obj));
            }

            public void Bind()
                => m_obj.Bind();

            void IDisposable.Dispose()
                => m_obj.Release();

            public void Downgrade()
                => m_obj.Downgrade();

            public void Release()
                => m_obj.Release();

            public void Unbind()
                => m_obj.Unbind();

            public UpgradeRequest Upgrade()
            {
                m_obj.Upgrade();
                return new UpgradeRequest(m_obj);
            }
        }
    }
}

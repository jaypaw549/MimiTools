using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace MimiTools.Sync
{
    public sealed class BarrierObj : ILockable
    {
        private Barrier64 _impl = new Barrier64();

        public Pass GetPass()
            => new Pass(this, _impl.EnterId());

        public bool TryGetPass(out Pass pass)
        {
            if (_impl.TryEnterId(out int id))
            {
                pass = new Pass(this, id);
                return true;
            }

            pass = default;

            return false;
        }

        ILock ILockable.GetLock()
            => GetPass();

        public readonly struct Pass : ILock
        {
            internal Pass(BarrierObj target, int id)
            {
                _id = id;
                _target = target;
            }

            public bool IsValid => _target?._impl.IsValidId(_id) ?? false;

            private readonly int _id;
            private readonly BarrierObj _target;

            public void Dispose()
                => _target?._impl.ExitId(_id);

            void ILock.Release()
                => Dispose();
        }
    }
}

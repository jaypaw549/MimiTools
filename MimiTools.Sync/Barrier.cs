using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace MimiTools.Sync
{
    public sealed class Barrier
    {
        private volatile int _active = 0;
        private volatile int _id = 0;

        public Pass GetPass()
        {
            Pass p;
            SpinWait spinner = new SpinWait();
            while (!TryGetPass(out p))
                spinner.SpinOnce();

            return p;
        }

        private void Release(int id)
        {
            //conditional increment, allows releasing only once per permitted pass
            if (Interlocked.CompareExchange(ref _id, id + 1, id) == id)
                _active = 0;
        }

        public bool TryGetPass(out Pass pass)
        {
            if (0 == Interlocked.Exchange(ref _active, 1))
            {
                pass = new Pass(this, _id);
                return true;
            }

            pass = default;

            return false;
        }

        public readonly struct Pass : IDisposable
        {
            internal Pass(Barrier target, int id)
            {
                _id = id;
                _target = target;
            }

            public bool IsValid { get => _target != null ? _target._id == _id : false; }

            private readonly int _id;
            private readonly Barrier _target;

            public void Dispose()
                => _target?.Release(_id);
        }
    }
}

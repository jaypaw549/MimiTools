using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace MimiTools.Sync
{
    public struct StructLock
    {
        private volatile int _lock;

        public void Enter()
        {
            SpinWait wait = new SpinWait();
            while (Interlocked.Exchange(ref _lock, 1) == 1)
                wait.SpinOnce();
        }

        public void Exit()
        {
            _lock = 0;
        }

        public bool TryEnter()
        {
            return Interlocked.Exchange(ref _lock, 1) == 0;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace MimiTools.Sync
{

    [StructLayout(LayoutKind.Sequential, Pack = sizeof(int), Size = sizeof(int) * 2)]
    public struct Barrier64
    {
        private volatile int _current;
        private volatile int _free;

        public Pass Enter()
            => new Pass(EnterId());

        public int EnterId()
        {
            SpinWait spinner = new SpinWait();
            int id = Interlocked.Increment(ref _free) - 1;
            while (_current != id)
                spinner.SpinOnce();

            return id;
        }

        public bool Exit(Pass pass)
            => pass.IsValid && ExitId(pass.Id);

        internal bool ExitId(int id)
            => id == Interlocked.CompareExchange(ref _current, id + 1, id);

        public bool IsValid(Pass pass)
            => pass.IsValid && _current == pass.Id;

        internal bool IsValidId(int id)
            => id == _current;

        public bool TryEnter(out Pass pass)
        {
            if (TryEnterId(out int id))
            {
                pass = new Pass(id);
                return true;
            }

            pass = default;
            return false;
        }

        internal bool TryEnterId(out int id)
        {
            id = _free;
            return id == _current && id == Interlocked.CompareExchange(ref _free, id + 1, id);
        }

        [StructLayout(LayoutKind.Sequential, Pack = sizeof(int), Size = sizeof(int) * 2)]
        public readonly struct Pass
        {
            private readonly int _check;
            private readonly int _id;
            public bool IsValid => ~_check == _id;

            public int Id => _id;

            internal Pass(int id)
            {
                _check = ~id;
                _id = id;
            }
        }
    }
}

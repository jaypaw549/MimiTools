using MimiTools.Tools;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace MimiTools.Sync
{
    /// <summary>
    /// <see cref="Barrier32"/> is a non-reentrant FiFo lock built on the size of an <see cref="int"/>.
    /// It is smaller than its 64-bit counterpart <see cref="Barrier64"/>, but is also slower due
    /// to needing to access values smaller than 32 bits. See <see cref="Value32"/> for how it does that.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = sizeof(int), Size = sizeof(int))]
    public struct Barrier32
    {
        //Short index 0 = current, Short index 1 = free
        private Value32 _state;

        /// <summary>
        /// Creates a pass representing you can enter the protected area.
        /// This blocks until it is your turn.
        /// </summary>
        /// <returns>a valid pass saying you can enter the protected area</returns>
        public Pass Enter()
            => new Pass(EnterId());

        internal short EnterId()
        {
            SpinWait spinner = new SpinWait();
            short id = (short)(_state.IncrementShort(1) - 1);
            while (_state.ReadShort(0) != id)
                spinner.SpinOnce();

            return id;
        }

        public bool Exit(Pass pass)
        {
            //conditional increment, allows releasing only once per permitted pass
            if (pass.IsValid)
                return ExitId(pass.Id);

            return false;
        }

        internal bool ExitId(short id)
        {
            return id == _state.CompareExchangeShort(0, (short)(id + 1), id);
        }

        public bool IsValid(Pass pass)
            => pass.IsValid && _state.ReadShort(0) == pass.Id;

        internal bool IsValidId(short id)
            => id == _state.ReadShort(0);

        public bool TryEnter(out Pass pass)
        {
            if (TryEnterId(out short id))
            {
                pass = new Pass(id);
                return true;
            }

            pass = default;
            return false;
        }

        internal bool TryEnterId(out short id)
        {
            id = _state.ReadShort(1);

            return id == _state.ReadShort(0) &&
                id == _state.CompareExchangeShort(1, (short)(id + 1), id);
        }

        [StructLayout(LayoutKind.Sequential, Pack = sizeof(short), Size = sizeof(short) * 2)]
        public readonly struct Pass
        {
            private readonly short _check;
            private readonly short _id;
            public bool IsValid => ~_check == _id;

            public short Id => _id;

            internal Pass(short id)
            {
                _check = (short)~id;
                _id = id;
            }
        }
    }
}

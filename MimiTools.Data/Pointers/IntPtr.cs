using System;
using System.Collections.Generic;
using System.Text;

namespace MimiTools.Data.Pointers
{
    public readonly struct IntPtr : IPointer<IntPtr>
    {
        public IntPtr(int value)
        {
            _value = value;
        }

        private IntPtr(long value)
        {
            checked
            {
                _value = (int)value;
            }
        }

        private readonly int _value;

        public int PtrSize => sizeof(int);

        public IntPtr AsPointer => this;

        public int IntValue => _value;

        public long LongValue => _value;

        public IntPtr Add(IntPtr other)
            => new IntPtr(_value + other._value);

        public IntPtr Add(int offset)
            => new IntPtr(_value + offset);

        public IntPtr Add(long offset)
            => new IntPtr(_value + (int) offset);

        public IntPtr Decrement()
            => new IntPtr(_value - sizeof(int));
        public IntPtr Increment()
            => new IntPtr(_value + sizeof(int));

        public IntPtr Multiply(int multiplier)
            => new IntPtr(_value * multiplier);

        public IntPtr Multiply(long multiplier)
            => new IntPtr(_value * multiplier);

        public IntPtr Subtract(IntPtr other)
            => new IntPtr(_value - other._value);

        public IntPtr Subtract(int offset)
            => new IntPtr(_value - offset);

        public IntPtr Subtract(long offset)
            => new IntPtr(_value - (int) offset);

        public override string ToString()
            => $"{nameof(IntPtr)}: {_value}";
    }
}

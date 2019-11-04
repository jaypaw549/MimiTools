using System;
using System.Collections.Generic;
using System.Text;

namespace MimiTools.Data.Pointers
{
    public readonly struct LongPtr
    {
        public LongPtr(long value)
        {
            _value = value;
        }

        private readonly long _value;

        public int PtrSize => sizeof(long);

        public int IntValue => (int) _value;

        public long LongValue => _value;

        public LongPtr Value => this;

        public LongPtr Add(LongPtr other)
            => new LongPtr(_value + other._value);

        public LongPtr Decrement()
            => new LongPtr(_value - sizeof(long));

        public LongPtr Increment()
            => new LongPtr(_value + sizeof(long));

        public LongPtr Multiply(int multiplier)
            => new LongPtr(_value * multiplier);

        public LongPtr Multiply(long multiplier)
            => new LongPtr(_value * multiplier);

        public LongPtr Subtract(LongPtr other)
            => new LongPtr(_value - other._value);

        public override string ToString()
            => $"{nameof(LongPtr)}: {_value}";
    }
}

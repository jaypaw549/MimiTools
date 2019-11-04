using System;
using System.Collections.Generic;
using System.Text;

namespace MimiTools.Data.Pointers
{
    public readonly struct ShortPtr : IPointer<ShortPtr>
    {
        public ShortPtr(short value)
        {
            _value = value;
        }

        private ShortPtr(int value)
        {
            checked
            {
                _value = (short)value;
            }
        }

        private ShortPtr(long value)
        {
            checked
            {
                _value = (short)value;
            }
        }

        private readonly short _value;

        public int PtrSize => sizeof(short);

        public ShortPtr AsPointer => this;

        public int IntValue => _value;

        public long LongValue => _value;

        public ShortPtr Add(ShortPtr other)
            => new ShortPtr(_value + other._value);

        public ShortPtr Add(int offset)
            => new ShortPtr(_value + offset);

        public ShortPtr Add(long offset)
            => new ShortPtr(_value + offset);

        public ShortPtr Decrement()
            => new ShortPtr(_value - sizeof(short));
        public ShortPtr Increment()
            => new ShortPtr(_value + sizeof(short));

        public ShortPtr Multiply(int multiplier)
            => new ShortPtr(_value * multiplier);
        public ShortPtr Multiply(long multiplier)
            => new ShortPtr(_value * multiplier);

        public ShortPtr Subtract(ShortPtr other)
            => new ShortPtr(_value - other._value);

        public ShortPtr Subtract(int offset)
            => new ShortPtr(_value - offset);

        public ShortPtr Subtract(long offset)
            => new ShortPtr(_value - offset);

        public override string ToString()
            => $"{nameof(ShortPtr)}: {_value}";
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace MimiTools.Data.Pointers
{
    public readonly struct BytePtr : IPointer<BytePtr>
    {
        public BytePtr(byte value)
        {
            _value = value;
        }

        private BytePtr(int value)
        {
            checked
            {
                _value = (byte)value;
            }
        }

        private BytePtr(long value)
        {
            checked
            {
                _value = (byte)value;
            }
        }

        private readonly byte _value;

        public BytePtr AsPointer => this;

        public int IntValue => _value;

        public long LongValue => _value;

        public int PtrSize => sizeof(byte);

        public  BytePtr Add(BytePtr other)
            => new BytePtr(_value + other._value);

        public BytePtr Add(int offset)
            => new BytePtr(_value + offset);

        public BytePtr Add(long offset)
            => new BytePtr(_value + offset);

        public BytePtr Decrement()
            => new BytePtr(_value - sizeof(byte));

        public BytePtr Increment()
            => new BytePtr(_value + sizeof(byte));

        public BytePtr Multiply(int multiplier)
            => new BytePtr(_value * multiplier);

        public BytePtr Multiply(long multiplier)
            => new BytePtr(_value * multiplier);

        public BytePtr Subtract(BytePtr other)
            => new BytePtr(_value - other._value);

        public BytePtr Subtract(int offset)
            => new BytePtr(_value - offset);

        public BytePtr Subtract(long offset)
            => new BytePtr(_value - offset);

        public override string ToString()
            => $"{nameof(BytePtr)}: {_value}";
    }
}

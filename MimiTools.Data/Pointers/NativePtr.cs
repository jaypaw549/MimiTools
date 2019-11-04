using System;
using System.Collections.Generic;
using System.Text;

namespace MimiTools.Data.Pointers
{
    public readonly struct NativePtr : IPointer<NativePtr>
    {
        public NativePtr(System.IntPtr value)
        {
            _value = value;
        }

        private readonly System.IntPtr _value;

        public int PtrSize => System.IntPtr.Size;

        public NativePtr AsPointer => this;

        public int IntValue => _value.ToInt32();

        public long LongValue => _value.ToInt64();

        public NativePtr Add(NativePtr other)
            => new NativePtr(new System.IntPtr(_value.ToInt64() + other._value.ToInt64()));

        public NativePtr Add(int offset)
            => new NativePtr(System.IntPtr.Add(_value, offset));

        public NativePtr Add(long offset)
            => new NativePtr(new System.IntPtr(_value.ToInt64() + offset));

        public NativePtr Decrement()
            => new NativePtr(System.IntPtr.Subtract(_value, System.IntPtr.Size));

        public NativePtr Increment()
            => new NativePtr(System.IntPtr.Add(_value, System.IntPtr.Size));

        public NativePtr Multiply(int multiplier)
            => new NativePtr(new System.IntPtr(_value.ToInt64() * multiplier));

        public NativePtr Multiply(long multiplier)
            => new NativePtr(new System.IntPtr(_value.ToInt64() * multiplier));

        public NativePtr Subtract(NativePtr other)
            => new NativePtr(new System.IntPtr(_value.ToInt64() - other._value.ToInt64()));

        public NativePtr Subtract(int offset)
            => new NativePtr(new System.IntPtr(_value.ToInt64() - offset));

        public NativePtr Subtract(long offset)
            => new NativePtr(new System.IntPtr(_value.ToInt64() - offset));

        public override string ToString()
            => $"{nameof(NativePtr)}: {_value}";
    }
}

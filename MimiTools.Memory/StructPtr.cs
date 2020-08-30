using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace MimiTools.Memory
{
    public readonly ref struct StructPtr<T> where T : unmanaged
    {
        public IntPtr Pointer { get; }

        public unsafe ref T Reference => ref *(T*)Pointer;

        public T Value { get => Reference; set => Reference = value; }

        public unsafe StructPtr(ref T target)
            => Pointer = new IntPtr(Unsafe.AsPointer(ref target));
    }
}

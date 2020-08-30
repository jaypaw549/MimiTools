using System;
using System.Collections.Generic;
using System.Text;

namespace MimiTools.Memory
{
    public readonly struct StructPtr<T> where T : unmanaged
    {
        public IntPtr Pointer { get; }

        public unsafe ref T Reference => ref *(T*)Pointer;

        public T Value { get => Reference; set => Reference = value; }

        public unsafe StructPtr(ref T target)
        {
            fixed (T* ptr = &target)
                Pointer = new IntPtr(ptr);
        }
    }
}

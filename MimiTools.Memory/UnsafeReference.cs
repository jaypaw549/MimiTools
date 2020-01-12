using System;
using System.Runtime.CompilerServices;

namespace MimiTools.Memory
{
    public static class UnsafeReference
    {
        public static UnsafeReference<T> FromReference<T>(in T target)
            => new UnsafeReference<T>(ref Unsafe.AsRef(in target));
    }

    public readonly struct UnsafeReference<T>
    {
        private readonly IntPtr m_ptr;

        public unsafe ref T Reference => ref Unsafe.AsRef<T>(m_ptr.ToPointer());

        public unsafe T Value { get => Unsafe.Read<T>(m_ptr.ToPointer()); set => Unsafe.Write(m_ptr.ToPointer(), value); }

        public unsafe UnsafeReference(ref T target)
            => m_ptr = new IntPtr(Unsafe.AsPointer(ref target));
    }
}

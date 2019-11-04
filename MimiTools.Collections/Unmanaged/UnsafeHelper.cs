using System;
using System.Runtime.InteropServices;

namespace MimiTools.Collections.Unmanaged
{

    internal class UnsafeHelper
    {
        internal unsafe static void* Alloc(long size)
            => (void*)Marshal.AllocHGlobal(new IntPtr(size));

        internal unsafe static T* AllocType<T>(long size) where T : unmanaged
            => (T*)Alloc(size * sizeof(T));

        internal unsafe static void Copy(void* src, void* dst, long size)
        {
            byte* b_src = (byte*)src;
            byte* b_dst = (byte*)dst;
            for (long l = 0; l < size; l++)
                b_dst[l] = b_src[l];
        }

        internal unsafe static void CopyType<T>(T* src, T* dst, long size) where T : unmanaged
            => Copy(src, dst, size * sizeof(T));

        internal unsafe static void Free(void* ptr)
            => Marshal.FreeHGlobal(new IntPtr(ptr));

        internal unsafe static void* Realloc(void* ptr, long size)
            => (void*)Marshal.ReAllocHGlobal(new IntPtr(ptr), new IntPtr(size));

        internal unsafe static T* ReallocType<T>(T* ptr, long size) where T : unmanaged
            => (T*)Realloc(ptr, size * sizeof(T));

        internal unsafe static void Zero(void* t, long count)
        {
            byte* ptr = (byte*)t;
            byte* end = ptr + count;
            for (; ptr < end; ptr++)
                *ptr = 0;
        }

        internal unsafe static void ZeroType<T>(T* ptr, long count) where T : unmanaged
            => Zero(ptr, count * sizeof(T));
    }
}

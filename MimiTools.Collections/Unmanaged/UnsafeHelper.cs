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
            void** ptr_src = (void**)src;
            void** ptr_dst = (void**)dst;

            long count = size / sizeof(void**);

            for (long l = 0; l < count; l++)
                ptr_dst[l] = ptr_src[l];

            byte* b_src = (byte*)&ptr_src[count];
            byte* b_dst = (byte*)&ptr_dst[count];
            count = size % sizeof(void**);

            for (long l = 0; l < count; l++)
                b_dst[l] = b_src[l];
        }

        internal unsafe static void CopyType<T>(T* src, T* dst, long size) where T : unmanaged
            => Copy(src, dst, size * sizeof(T));

        internal unsafe static void Free(void* ptr)
            => Marshal.FreeHGlobal(new IntPtr(ptr));

        internal unsafe static bool IsDefault(void* ptr, long size)
        {
            void** p_ptr = (void**)ptr;
            long count = size / sizeof(void*);

            for (long i = 0; i < count; i++)
                if (p_ptr[i] != null)
                    return false;

            byte* b_ptr = (byte*)&p_ptr[count];
            count = size % sizeof(void*);

            for (long i = 0; i < count; i++)
                if (b_ptr[i] != 0)
                    return false;

            return true;
        }

        internal unsafe static void* Realloc(void* ptr, long size)
            => (void*)Marshal.ReAllocHGlobal(new IntPtr(ptr), new IntPtr(size));

        internal unsafe static T* ReallocType<T>(T* ptr, long size) where T : unmanaged
            => (T*)Realloc(ptr, size * sizeof(T));

        internal unsafe static void Zero(void* t, long size)
        {
            void** p_ptr = (void**)t;

            long count = size / sizeof(void**);

            for (long l = 0; l < count; l++)
                p_ptr[l] = null;

            byte* b_ptr = (byte*)&p_ptr[count];
            count = size % sizeof(void**);

            for (long l = 0; l < count; l++)
                b_ptr[l] = 0;
        }

        internal unsafe static void ZeroType<T>(T* ptr, long count) where T : unmanaged
            => Zero(ptr, count * sizeof(T));
    }
}

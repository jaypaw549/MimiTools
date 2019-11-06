using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace MimiTools.Memory
{
    public readonly struct UnmanagedMemory : IDisposable
    {
        public readonly IntPtr Pointer { get; }

        public UnmanagedMemory(int size)
        {
            Pointer = Marshal.AllocHGlobal(size);
        }

        public UnmanagedMemory(int size, out IntPtr ptr)
        {
            ptr = Pointer = Marshal.AllocHGlobal(size);
        }

        public void Dispose()
        {
            Marshal.FreeHGlobal(Pointer);
        }

        public static UnmanagedMemory Alloc(int size, out IntPtr ptr)
            => new UnmanagedMemory(size, out ptr);

        public unsafe static UnmanagedMemory Alloc<T>(int count, out void* ptr) where T : unmanaged
        {
            UnmanagedMemory memory = new UnmanagedMemory(sizeof(T) * count, out IntPtr t_ptr);
            ptr = t_ptr.ToPointer();
            return memory;
        }

        public unsafe static UnmanagedMemory Alloc<T>(int count, out T* ptr) where T : unmanaged
        {
            UnmanagedMemory memory = new UnmanagedMemory(sizeof(T) * count, out IntPtr t_ptr);
            ptr = (T*)t_ptr;
            return memory;
        }
    }
}

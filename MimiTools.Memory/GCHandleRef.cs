using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace MimiTools.Memory
{
    public struct GCHandleRef
    {
        public bool IsAllocated => _handle != IntPtr.Zero;
        private IntPtr _handle;

        public GCHandleRef(GCHandle handle)
        {
            if (!handle.IsAllocated)
                throw new ArgumentException("Invalid Handle!");
            _handle = GCHandle.ToIntPtr(handle);
        }

        public GCHandleRef(object value, GCHandleType type)
        {
            _handle = GCHandle.ToIntPtr(GCHandle.Alloc(value, type));
        }

        public GCHandleRef(IntPtr ptr)
        {
            _handle = ptr;
        }

        public void Free()
            => GCHandle.FromIntPtr(Interlocked.Exchange(ref _handle, IntPtr.Zero)).Free();

        public unsafe ref T ObjectReference<T>() where T : class
            => ref Unsafe.AsRef<T>(_handle.ToPointer());

        public ref T ValueReference<T>() where T : struct
            => ref Unsafe.Unbox<T>(ObjectReference<object>());

        public static void VolatileRead(ref GCHandleRef handle_ref)
            => new GCHandleRef(Volatile.Read(ref handle_ref._handle));

        public static void VolatileWrite(ref GCHandleRef handle_ref, GCHandleRef value)
            => Volatile.Write(ref handle_ref._handle, value._handle);
    }
}

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace MimiTools.Memory
{
    public struct GCHandleRef
    {
        public bool IsAllocated => m_handle != IntPtr.Zero;
        public IntPtr Handle => m_handle;
        private IntPtr m_handle;

        public GCHandleRef(GCHandle handle)
        {
            if (!handle.IsAllocated)
                throw new ArgumentException("Invalid Handle!");
            m_handle = GCHandle.ToIntPtr(handle);
        }

        public GCHandleRef(object value, GCHandleType type)
        {
            m_handle = GCHandle.ToIntPtr(GCHandle.Alloc(null, type));
            ObjectReference<object>() = value;
        }

        public GCHandleRef(IntPtr ptr)
        {
            m_handle = ptr;
        }

        public void Free()
            => GCHandle.FromIntPtr(Interlocked.Exchange(ref m_handle, IntPtr.Zero)).Free();

        public unsafe ref T ObjectReference<T>() where T : class
            => ref Unsafe.AsRef<T>(m_handle.ToPointer());

        public ref T ValueReference<T>() where T : struct
            => ref Unsafe.Unbox<T>(ObjectReference<object>());

        public static void VolatileRead(ref GCHandleRef handle_ref)
            => new GCHandleRef(Volatile.Read(ref handle_ref.m_handle));

        public static void VolatileWrite(ref GCHandleRef handle_ref, GCHandleRef value)
            => Volatile.Write(ref handle_ref.m_handle, value.m_handle);
    }
}

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace MimiTools.Collections.Unmanaged
{
    internal static class UnsafeDebugHelper
    {
        private static readonly Random _noise = new Random();
        private static readonly Dictionary<IntPtr, GCHandle> _handles = new Dictionary<IntPtr, GCHandle>();

        internal unsafe static void* Alloc(long size)
        {
            byte[] data = new byte[size];
            _noise.NextBytes(data);

            GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            _handles.Add(handle.AddrOfPinnedObject(), handle);

            return (void*)handle.AddrOfPinnedObject();
        }

        internal unsafe static T* AllocType<T>(long size) where T : unmanaged
            => (T*)Alloc(size * sizeof(T));

        internal unsafe static void Free(void* ptr)
        {
            if (_handles.TryGetValue(new IntPtr(ptr), out GCHandle handle))
            {
                handle.Free();
                return;
            }
            throw new InvalidOperationException();
        }

        internal unsafe static byte[] GetBackend(void* ptr)
        {
            if (!_handles.TryGetValue(new IntPtr(ptr), out GCHandle handle))
                throw new InvalidOperationException("Target pointer wasn't allocated with this class!");

            return (byte[])handle.Target;
        }

        internal unsafe static void* Realloc(void* ptr, long size)
        {
            if (!_handles.TryGetValue(new IntPtr(ptr), out GCHandle handle))
                throw new InvalidOperationException();

            byte[] old = (byte[])handle.Target;
            byte[] data = new byte[size];
            _noise.NextBytes(data);

            Array.Copy(old, data, size > old.LongLength ? old.LongLength : size);
            handle.Target = data;

            _handles.Remove(new IntPtr(ptr));
            _handles.Add(handle.AddrOfPinnedObject(), handle);

            return (void*)handle.AddrOfPinnedObject();
        }

        internal unsafe static T* ReallocType<T>(T* ptr, long size) where T : unmanaged
            => (T*)Realloc(ptr, size * sizeof(T));
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace MimiTools.Collections.Unmanaged
{
    public unsafe readonly ref struct UnmanagedDisposable<T> where T : unmanaged, IDisposable
    {
        public UnmanagedDisposable(T* ptr)
            => target = ptr;
        
        private readonly T* target;

        public void Dispose()
            => target->Dispose();
    }
}

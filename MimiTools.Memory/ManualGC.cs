using System;
using System.Runtime.InteropServices;

namespace MimiTools.Memory
{
    public unsafe struct ManualGC
    {
        private Header* First;

        public TypedPointer<T> Alloc<T>() where T : unmanaged, IDisposable
        {
            int size = Marshal.SizeOf<T>() + Header.Size;
            Header* ptr = (Header*)Marshal.AllocHGlobal(size).ToPointer();
            ptr->Next = First;
            First = ptr;

            return new TypedPointer<T>((T*)(ptr + 1));
        }

        public void FreeAll()
        {
            for (Header* ptr = First; ptr != null; ptr = ptr->Next)
            {
                Marshal.FreeHGlobal(new IntPtr(ptr));
            }

            First = null;
        }

        public TypedPointer<T> Store<T>(in T value) where T : unmanaged, IDisposable
        {
            TypedPointer<T> r = Alloc<T>();
            *r.Unmanaged = value;
            return r;
        }
    }

    internal unsafe struct Header
    {
        internal static readonly int Size = Marshal.SizeOf<Header>();
        internal Header* Prev;
        internal Header* Next;
        //internal RuntimeTypeHandle Handle;
    }

    public unsafe struct Pointer
    {
        internal Pointer(void* ptr)
        {
            this.ptr = ptr;
        }

        private readonly void* ptr;

        public T* GetPointer<T>() where T : unmanaged, IDisposable
            => (T*)ptr;

        public ref T GetReference<T>() where T : unmanaged, IDisposable
            => ref *(T*)ptr;

        public T GetValue<T>() where T : unmanaged, IDisposable
            => *(T*)ptr;
    }

    public unsafe struct TypedPointer<T> : IDisposable where T : unmanaged, IDisposable
    {
        internal TypedPointer(T* ptr)
        {
            this.ptr = ptr;
        }

        private readonly T* ptr;

        public ref T Managed { get => ref *ptr; }
        public T* Unmanaged { get => ptr; }
        public T Value { get => *ptr; set => *ptr = value; }

        void IDisposable.Dispose()
        {
            if (Unmanaged == null)
                return;

            Unmanaged->Dispose();
            Header* ptr = ((Header*)Unmanaged) - 1;
            ptr->Prev->Next = ptr->Next;
            ptr->Next->Prev = ptr->Prev;

            Marshal.FreeHGlobal(new IntPtr(ptr));
        }

        public static implicit operator Pointer(TypedPointer<T> r)
            => new Pointer(r.ptr);

        public static explicit operator TypedPointer<T>(Pointer r)
            => new TypedPointer<T>(r.GetPointer<T>());
    }
}

using MimiTools.Sync;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace MimiTools.Memory
{
    public partial struct SafeGCHandle : IDisposable
    {
        private static ObjectPoolStruct<PoolItem> _pinned = new ObjectPoolStruct<PoolItem>(5);
        private static ObjectPoolStruct<PoolItem> _normal = new ObjectPoolStruct<PoolItem>(5);
        private static ObjectPoolStruct<PoolItem> _weak = new ObjectPoolStruct<PoolItem>(5);
        private static ObjectPoolStruct<PoolItem> _weak_track = new ObjectPoolStruct<PoolItem>(5);

        public static int PinnedPoolSize { get => _pinned.Limit; set => _pinned.Limit = value; }
        public static int NormalPoolSize { get => _normal.Limit; set => _normal.Limit = value; }
        public static int WeakPoolSize { get => _weak.Limit; set => _weak.Limit = value; }
        public static int WeakTrackResurrectionPoolSize { get => _weak_track.Limit; set => _weak_track.Limit = value; }

        private static PoolItem Alloc(object obj, GCHandleType type)
        {
            if (GetPool(type).TryRemove(out PoolItem item))
                item.SetTarget(obj, item.Generation);
            else
                item = new PoolItem(obj, type);

            return item;
        }

        private static ref ObjectPoolStruct<PoolItem> GetPool(GCHandleType type)
        {
            switch (type)
            {
                case GCHandleType.Pinned:
                    return ref _pinned;
                case GCHandleType.Normal:
                    return ref _normal;
                case GCHandleType.Weak:
                    return ref _weak;
                case GCHandleType.WeakTrackResurrection:
                    return ref _weak_track;
                default:
                    break;
            }
            throw new ArgumentException(nameof(type));
        }

        private static void Recycle(PoolItem handle)
        {
            if (handle == null)
                return;

            if (GetPool(handle._type).TryAdd(handle))
                GC.ReRegisterForFinalize(handle);
            else
            {
                GC.SuppressFinalize(handle);
                handle.Release();
            }
        }

        private class PoolItem : IStackable<PoolItem>
        {
            internal PoolItem(object obj, GCHandleType type)
            {
                _handle = GCHandle.ToIntPtr(GCHandle.Alloc(obj, type));
                _type = type;
                GC.KeepAlive(this);
            }

            internal int Generation => _gen;

            internal event Action<object, int> OnUnreachable;

            private readonly IntPtr _handle;
            internal readonly GCHandleType _type;

            private volatile int _gen = 0;

            //Not modified while on the stack, modified before and after.
            private volatile PoolItem _next;

            ref PoolItem IStackable<PoolItem>.Next => ref _next;

            private void Invoke(object obj)
            {
                Interlocked.Exchange(ref OnUnreachable, null)?.Invoke(obj, GC.GetGeneration(this));
            }

            internal object GetTarget(int gen)
            {
                if (gen != _gen)
                    throw new InvalidOperationException();

                GCHandle handle = GCHandle.FromIntPtr(_handle);
                return handle.Target;
            }

            internal ref T GetObjectReference<T>(int gen) where T : class
            {
                if (_gen != gen)
                    throw new InvalidOperationException();

                GCHandleRef gc_ref = new GCHandleRef(_handle);
                return ref gc_ref.ObjectReference<T>();
            }

            internal ref T GetValueReference<T>(int gen) where T : struct
            {
                if (_gen != gen)
                    throw new InvalidOperationException();

                GCHandleRef gc_ref = new GCHandleRef(_handle);
                return ref gc_ref.ValueReference<T>();
            }

            internal void Release()
            {
                Interlocked.Increment(ref _gen);
                GCHandle.FromIntPtr(_handle).Free();
            }

            internal void SetTarget(object value, int gen)
            {
                if (gen != _gen)
                    throw new InvalidOperationException();

                GCHandle handle = GCHandle.FromIntPtr(_handle);
                handle.Target = _handle;
            }

            void IStackable<PoolItem>.Reset(bool live)
            {
                if (!live)
                    Interlocked.Increment(ref _gen);

                SetTarget(null, _gen);
                _next = null;
            }

            ~PoolItem()
            {
                if (Environment.HasShutdownStarted)
                {
                    Release();
                    return;
                }

                ThreadPool.UnsafeQueueUserWorkItem(Invoke, GetTarget(_gen));
                Recycle(this);
            }
        }
    }
}

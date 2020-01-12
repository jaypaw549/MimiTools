using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace MimiTools.Memory
{
    public partial struct SafeGCHandle
    {
        public SafeGCHandle(object obj)
        {
            if (obj is PoolItem item)
                _item = item;
            else
                _item = Alloc(obj, GCHandleType.Normal);
            _gen = _item.Generation;
        }

        public SafeGCHandle(object obj, GCHandleType type)
        {
            _item = Alloc(obj, type);
            _gen = _item.Generation;
        }

        public event Action<object, int> OnHandleUnreachable
        {
            add
            {
                _item.OnUnreachable += value;
            }
            remove
            {
                _item.OnUnreachable -= value;
            }
        }

        private readonly int _gen;
        private volatile PoolItem _item;

        public object Target
        {
            get => _item.GetTarget(_gen);
            set => _item.SetTarget(value, _gen);
        }

        public void Dispose()
           => Recycle(Interlocked.Exchange(ref _item, null));

        public void KeepAlive()
            => GC.KeepAlive(_item);

        public void Free()
            => Recycle(Interlocked.Exchange(ref _item, null));

        public int GetHandleGeneration()
            => GC.GetGeneration(_item);

        public ref T UnsafeGetObjectReference<T>() where T : class
            => ref _item.GetObjectReference<T>(_gen);

        public ref T UnsafeGetValueReference<T>() where T : struct
            => ref _item.GetValueReference<T>(_gen);
    }
}

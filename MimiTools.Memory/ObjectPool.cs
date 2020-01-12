using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace MimiTools.Memory
{
    public class ObjectPool<T> where T : class, IStackable<T>
    {
        public int Items => _pool.Items;

        public int Limit { get => _pool.Limit; set => _pool.Limit = value; }

        private ObjectPoolStruct<T> _pool;

        public ObjectPool(int limit)
        {
            _pool = new ObjectPoolStruct<T>(limit);
        }

        public bool TryAdd(T obj)
            => _pool.TryAdd(obj);

        public bool TryRemove(out T obj)
            => _pool.TryRemove(out obj);
    }
}

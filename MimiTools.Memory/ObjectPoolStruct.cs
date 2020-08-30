using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace MimiTools.Memory
{
    public struct ObjectPoolStruct<T> where T : class, IStackable<T>
    {
        private volatile int _count;
        private volatile int _limit;

        public int Items => _count;

        public int Limit { get => _limit; set => _limit = value; }

        private volatile T _stack;

        public ObjectPoolStruct(int limit)
        {
            _count = 0;
            _limit = limit;
            _stack = null;
        }

        public int DecreaseLimit(int amount)
            => Interlocked.Add(ref _limit, -amount);

        public int IncreaseLimit(int amount)
            => Interlocked.Add(ref _limit, amount);

        public bool TryAdd(T obj)
        {
            if (!TryReserveSlot())
                return false;

            obj.Reset(false);
            T item = _stack;
            while (true)
            {
                Volatile.Write(ref obj.Next, item);
                T ret = Interlocked.CompareExchange(ref _stack, obj, item);
                if (ReferenceEquals(ret, item))
                    break;
                item = ret;
            }
            return true;
        }

        private bool TryReserveSlot()
        {
            int count = _count;
            while (count < _limit)
            {
                int ret = Interlocked.CompareExchange(ref _count, count + 1, count);
                if (ret == count)
                    return true;

                count = ret;
            }
            return false;
        }

        public bool TryRemove(out T obj)
        {
            T item = _stack;
            while (true)
            {
                if (item == null)
                    break;

                T ret = Interlocked.CompareExchange(ref _stack, Volatile.Read(ref item.Next), item);

                if (ReferenceEquals(ret, item))
                    break;

                item = ret;
            }

            if (item != null)
            {
                Interlocked.Decrement(ref _count);
                Volatile.Write(ref item.Next, null);
                item.Reset(true);
            }

            obj = item;
            return obj != null;
        }
    }
}

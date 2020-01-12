using MimiTools.Sync;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace MimiTools.Memory
{
    public class SoftReference<T>
    {
        public SoftReference(T value, int generation)
        {
            object obj = value;
            _gen = generation;
            _value = new SafeGCHandle(obj, GCHandleType.Weak);

            CheckUsage(obj, 0);
            GC.KeepAlive(this);
        }

        private readonly int _gen;

        private SafeGCHandle _value;
        
        private void CheckUsage(object obj, int gen)
        {
            //Unless a GC collection has occurred of the generation we want, or the object is already the generation we want, keep the object alive.
            //This will naturally gravitate towards promoting the object to the max GC generation.

            //Ideally we'd somehow detect GC and clear the strong reference before, but there's no GC hooks so we have to clear after-the-fact.
            if (gen < _gen && GC.GetGeneration(obj) < gen && ReferenceEquals(obj, _value.Target))
                new SafeGCHandle(obj, GCHandleType.Normal).OnHandleUnreachable += CheckUsage;
        }

        public void SetTarget(T value)
        {
            object obj = value;
            _value.Target = obj;
            CheckUsage(obj, 0);
        }

        public bool TryGetTarget(out T value)
        {
            object obj = _value.Target;
            if (obj != null && obj is T)
            {
                value = (T)obj;
                return true;
            }
            value = default;
            return false;
        }
    }
}

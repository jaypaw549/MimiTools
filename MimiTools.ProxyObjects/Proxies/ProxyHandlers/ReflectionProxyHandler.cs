using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;

namespace MimiTools.ProxyObjects.Proxies.ProxyHandlers
{
    internal class ReflectionProxyHandler : IProxyHandler
    {
        internal static ReflectionProxyHandler Instance { get; } = new ReflectionProxyHandler();

        private ReflectionProxyHandler() { }

        private long _current = -1;
        private readonly Dictionary<long, object> _bindings = new Dictionary<long, object>();

        public long BindObject(object obj)
        {
            long id = Interlocked.Increment(ref _current);
            _bindings[id] = obj;
            return id;
        }

        public object Invoke(ProxyObject obj, MethodInfo method, object[] args)
            => method.Invoke(_bindings[obj.Id], args);

        public bool CheckProxy(long id, Type contract_type)
        {
            if (!_bindings.TryGetValue(id, out object value))
                return false;

            return contract_type.IsInstanceOfType(value);
        }

        public void Release(long id, Type contractType)
            => _bindings.Remove(id);
    }
}

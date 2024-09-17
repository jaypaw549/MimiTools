using System;
using System.Reflection;

namespace MimiTools.ProxyObjects.Proxies
{
    public static class ReflectionProxy
    {
        public static object CreateProxy(Type t, object obj)
            => ProxyFactory.AbstractOnly.FromReference(t, new ReflectionReference(t));

        public static T CreateProxy<T>(T obj) where T : class
            => ProxyFactory.AbstractOnly.FromReference<T>(new ReflectionReference(obj));

        private static readonly ReflectionContract _contract = new ReflectionContract();

        private class ReflectionContract : IProxyContract
        {
            internal ReflectionContract()
            {
            }

            public object Invoke(ref ProxyReference obj, MethodInfo method, object[] args)
            {
                if (obj is ReflectionReference verified_obj)
                    return method.Invoke(verified_obj.Target, args);

                throw new InvalidOperationException($"{nameof(obj)} is not a valid reference for this contract!");
            }

            public void Release(ProxyReference obj) { }

            public bool Verify(ProxyReference obj)
                => ReferenceEquals(this, obj.Contract);
        }

        private class ReflectionReference(object obj) : ProxyReference(_contract)
        {
            internal readonly object Target = obj;
        }
    }
}

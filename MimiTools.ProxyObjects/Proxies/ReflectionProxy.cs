using System;
using System.Reflection;

namespace MimiTools.ProxyObjects.Proxies
{
    public static class ReflectionProxy
    {
        public static object Create(Type t, object obj)
            => ProxyFactory.Default.FromContract(t, new ReflectionContract(t));

        public static T Create<T>(T obj) where T : class
            => ProxyFactory.Default.FromContract<T>(new ReflectionContract(obj));

        private class ReflectionContract : IProxyContract
        {
            internal ReflectionContract(object instance)
            {
                _instance = instance;
            }

            private readonly object _instance;

            public object Invoke(ref IProxyContract contract, MethodInfo method, object[] args)
                => method.Invoke(_instance, args);

            public void Release()
            {
            }

            public bool Verify(Type t)
                => t?.IsInstanceOfType(_instance) ?? false;
        }
    }
}

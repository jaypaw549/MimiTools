using MimiTools.ProxyObjects.Proxies.Helpers;
using System;
using System.Reflection;

namespace MimiTools.ProxyObjects.Proxies
{
    public static class DynamicProxy
    {
        /// <summary>
        /// Creates a transparent proxy.
        /// </summary>
        /// <typeparam name="T">The interface type to create a proxy of</typeparam>
        /// <param name="obj">The object to create a proxy of</param>
        /// <param name="perms">The permissions to pass to the proxy object</param>
        /// <returns>A proxy object representing of the interfaces on the object.</returns>
        public static T Create<T>(T obj) where T : class
            => ProxyFactory.Default.FromContract<T>(new DynamicContract(new DynamicHelper(), obj));

        public static IProxyContract CreateContract(object obj)
            => new DynamicContract(new DynamicHelper(), obj);

        public static Func<T, IProxyContract> CreateContractFactory<T>()
        {
            DynamicHelper handler = new DynamicHelper();
            return obj => new DynamicContract(handler, obj);
        }

        /// <summary>
        /// Create a transparent proxy factory for the specified interface type, 
        /// recommended if you plan on making multiple proxies of the same type, as they reuse the same handler.
        /// 
        /// The provided handler generates DynamicMethods to speed up the invocation of methods on the object, and reusing the handler
        /// prevents the need to regenerate the code for each proxy
        /// </summary>
        /// <typeparam name="T">The interface type to create proxies for</typeparam>
        /// <returns>A function that creates transparent proxies implementing the specified type</returns>
        public static Func<T, T> CreateFactory<T>() where T : class
        {
            DynamicHelper handler = new DynamicHelper();
            return obj => ProxyFactory.Default.FromContract<T>(new DynamicContract(handler, obj));
        }

        private class DynamicContract : IProxyContract
        {
            private readonly DynamicHelper handler;
            private readonly object obj;

            public DynamicContract(DynamicHelper handler, object obj)
            {
                this.handler = handler;
                this.obj = obj;
            }

            public object Invoke(ref IProxyContract contract, MethodInfo method, object[] args)
                => handler.GetMethod(method).Invoke(obj, args);

            public void Release()
            {
            }

            public bool Verify(Type t)
                => t.IsInstanceOfType(obj);
        }
    }
}

using MimiTools.ProxyObjects.Proxies.ProxyHandlers;
using System;

namespace MimiTools.ProxyObjects.Proxies
{
    public static class DynamicProxy
    {
        /// <summary>
        /// Creates a transparent proxy, can only create proxies for interfaces. Any attempts to create proxies for classes will result in an exception
        /// </summary>
        /// <typeparam name="T">The interface type to create a proxy of</typeparam>
        /// <param name="obj">The object to create a proxy of</param>
        /// <param name="perms">The permissions to pass to the proxy object</param>
        /// <returns>A proxy object representing of the interfaces on the object.</returns>
        public static T Create<T>(T obj) where T : class
        {
            DynamicProxyHandler handler = new DynamicProxyHandler();
            return ProxyFactory<T>.CreateProxy(handler, handler.BindObject(obj));
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
        public static Func<T, T> CreateTransparentFactory<T>() where T : class
        {
            DynamicProxyHandler handler = new DynamicProxyHandler();
            return obj => ProxyFactory.Default.CreateProxy(handler, handler.BindObject(obj));
        }
    }
}

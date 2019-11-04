using MimiTools.ProxyObjects.Proxies.ProxyHandlers;
using System;

namespace MimiTools.ProxyObjects.Proxies
{
    public static class DynamicProxy
    {
        /// <summary>
        /// Creates an opaque proxy, proxies created this way do not implement the type they represent.
        /// </summary>
        /// <param name="obj">The object to create a proxy for</param>
        /// <param name="perms">The permissions to give the proxy object</param>
        /// <returns>A proxy object representing the object</returns>
        public static ProxyObject CreateOpaque(object obj)
        {
            DynamicProxyHandler handler = new DynamicProxyHandler();
            return new ProxyObject(obj.GetType(), handler.BindObject(obj), handler);
        }

        /// <summary>
        /// Creates a transparent proxy, can only create proxies for interfaces. Any attempts to create proxies for classes will result in an exception
        /// </summary>
        /// <typeparam name="T">The interface type to create a proxy of</typeparam>
        /// <param name="obj">The object to create a proxy of</param>
        /// <param name="perms">The permissions to pass to the proxy object</param>
        /// <returns>A proxy object representing of the interfaces on the object.</returns>
        public static T CreateTransparent<T>(T obj) where T : class
        {
            DynamicProxyHandler handler = new DynamicProxyHandler();
            return ProxyGenerator<T>.CreateProxy(handler.BindObject(obj), handler);
        }

        /// <summary>
        /// Create an opaque proxy factory, recommended if you plan on making multiple proxies of the same type, as they reuse the same handler.
        /// 
        /// The provided handler generates DynamicMethods to speed up the invocation of methods on the object, and reusing the handler
        /// prevents the need to regenerate the code for each proxy
        /// </summary>
        /// <returns>a function that creates ProxyObjects</returns>
        public static Func<object, ProxyObject> CreateOpaqueFactory()
        {
            DynamicProxyHandler handler = new DynamicProxyHandler();
            return obj => new ProxyObject(obj.GetType(), handler.BindObject(obj), handler);
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
            return obj => ProxyGenerator<T>.CreateProxy(handler.BindObject(obj), handler);
        }
    }
}

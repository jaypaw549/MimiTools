using MimiTools.ProxyObjects.Proxies.ProxyHandlers;

namespace MimiTools.ProxyObjects.Proxies
{
    public static class ReflectionProxy
    {
        public static ProxyObject CreateOpaque(object obj)
            => new ProxyObject(obj.GetType(), ReflectionProxyHandler.Instance.BindObject(obj), ReflectionProxyHandler.Instance);

        public static T CreateTransparent<T>(T obj) where T : class
            => ProxyGenerator<T>.CreateProxy(ReflectionProxyHandler.Instance.BindObject(obj), ReflectionProxyHandler.Instance);
    }
}

using System;
using System.Reflection;

namespace MimiTools.ProxyObjects
{
    public interface IProxyContract
    {
        public object Invoke(ref ProxyReference obj, MethodInfo method, object[] args);

        public void Release(ProxyReference obj);

        public bool Verify(ProxyReference obj);
    }
}
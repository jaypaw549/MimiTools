using System;
using System.Reflection;

namespace MimiTools.ProxyObjects
{
    public interface IProxyHandler
    {
        object Invoke(ProxyObject instance, MethodInfo method, object[] args);

        bool CheckProxy(long id, Type contract_type);

        void Release(long id, Type contractType);
    }
}
using System;
using System.Reflection;

namespace MimiTools.ProxyObjects
{
    public interface IProxyContract
    {
        public object Invoke(ref IProxyContract contract, MethodInfo method, object[] args);

        public void Release();

        public bool Verify(Type t);
    }
}
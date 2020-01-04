using System;
using System.Reflection;

namespace MimiTools.ProxyObjects
{
    public interface IProxyHandler
    {
        IProxyContract GetContract(long id, Type contract_type);
    }
}
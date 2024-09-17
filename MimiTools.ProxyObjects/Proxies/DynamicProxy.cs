using MimiTools.ProxyObjects.Proxies.Helpers;
using System;
using System.Diagnostics.Contracts;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace MimiTools.ProxyObjects.Proxies
{
    public static class DynamicProxy
    {
        public static T CreateProxy<T>(T obj) where T : class
            => ProxyFactory.OverrideVirtual.FromReference<T>(new DynamicReference(new DynamicContract(), obj));

        public static Func<T, T> CreateProxyFactory<T>() where T : class
        {
            DynamicContract contract = new DynamicContract();
            return obj => ProxyFactory.OverrideVirtual.FromReference<T>(new DynamicReference(contract, obj));
        }

        private class DynamicContract : IProxyContract
        {
            private readonly DynamicHelper handler = new DynamicHelper();

            public object Invoke(ref ProxyReference obj, MethodInfo method, object[] args)
            {
                if (obj is DynamicReference verified_obj)
                    return handler.GetMethod(method).Invoke(verified_obj.Target, args);

                throw new InvalidOperationException($"{nameof(obj)} is not a valid reference for this contract!");
            }

            public void Release(ProxyReference obj)
            {

            }

            public bool Verify(ProxyReference obj)
                => obj is DynamicReference && ReferenceEquals(this, obj.Contract);
        }

        private class DynamicReference(IProxyContract contract, object target) : ProxyReference(contract)
        {
            internal readonly object Target = target;
        }
    }
}

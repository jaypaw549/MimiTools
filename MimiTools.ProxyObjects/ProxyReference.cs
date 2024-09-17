using System;
using System.Collections.Generic;
using System.Text;

namespace MimiTools.ProxyObjects
{
    public class ProxyReference(IProxyContract contract)
    {
        public IProxyContract Contract { get; } = contract ?? throw new ArgumentNullException(nameof(contract));

        public static ProxyReference GetFromProxy(object proxy)
            => ProxyFactory.GetReferenceFromProxy(proxy);
    }
}

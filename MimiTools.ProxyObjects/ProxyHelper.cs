using System;
using System.Reflection;

namespace MimiTools.ProxyObjects
{
    internal static class ProxyHelper
    {
        internal static MethodInfo CreateContractMethod { get; } = typeof(IProxyHandler).GetMethod(nameof(IProxyHandler.GetContract));

        internal static MethodInfo GetMethodOperation { get; } = typeof(MethodBase).GetMethod(nameof(MethodBase.GetMethodFromHandle), new Type[] {
            typeof(RuntimeMethodHandle),
            typeof(RuntimeTypeHandle)
        });

        internal static MethodInfo InvokeMethod { get; } = typeof(IProxyContract).GetMethod(nameof(IProxyContract.Invoke));

        internal static MethodInfo ReleaseMethod { get; } = typeof(IProxyContract).GetMethod(nameof(IProxyContract.Release));

        internal static MethodInfo TypeOfOperation { get; } = typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle));
    }
}

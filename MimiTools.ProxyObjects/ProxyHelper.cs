using System;
using System.Reflection;

namespace MimiTools.ProxyObjects
{
    internal static class ProxyHelper
    {
        internal static ConstructorInfo ArgumentNullException { get; } = typeof(ArgumentNullException).GetConstructor(new Type[] { typeof(string) });

        internal static ConstructorInfo InvalidOperationException { get; } = typeof(InvalidOperationException).GetConstructor(new Type[] { typeof(string) });

        internal static MethodInfo ContractPropertyGetMethod { get; } = typeof(ProxyReference).GetProperty(nameof(ProxyReference.Contract)).GetMethod;

        internal static MethodInfo GetMethodOperation { get; } = typeof(MethodBase).GetMethod(nameof(MethodBase.GetMethodFromHandle), new Type[] {
            typeof(RuntimeMethodHandle),
            typeof(RuntimeTypeHandle)
        });

        internal static MethodInfo InvokeMethod { get; } = typeof(IProxyContract).GetMethod(nameof(IProxyContract.Invoke));

        internal static MethodInfo ReleaseMethod { get; } = typeof(IProxyContract).GetMethod(nameof(IProxyContract.Release));

        internal static MethodInfo TypeOfOperation { get; } = typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle));

        internal static MethodInfo VerifyMethod { get; } = typeof(IProxyContract).GetMethod(nameof(IProxyContract.Verify));
    }
}

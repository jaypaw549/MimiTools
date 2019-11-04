using System;
using System.Reflection;

namespace MimiTools.ProxyObjects
{
    internal static class ProxyHelper
    {
        internal static MethodInfo GetMethodOperation { get; } = typeof(MethodBase).GetMethod(nameof(MethodBase.GetMethodFromHandle), new Type[] {
            typeof(RuntimeMethodHandle),
            typeof(RuntimeTypeHandle)
        });

        internal static MethodInfo TypeOfOperation { get; } = typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle));
    }
}

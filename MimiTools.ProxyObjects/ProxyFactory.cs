﻿using MimiTools.Collections.Weak;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace MimiTools.ProxyObjects
{
    public class ProxyFactory
    {
        public static ProxyFactory Default { get; } = new ProxyFactory(false);

        public ProxyFactory(bool override_virtual)
        {
            _override_virtual = override_virtual;
        }

        private readonly Dictionary<Type, FactoryContainer> _containers = new Dictionary<Type, FactoryContainer>();
        private readonly bool _override_virtual;

        /// <summary>
        /// Create a transparent proxy using the specified id, permissions, and handler.
        /// </summary>
        /// <param name="id">The id to make the proxy with</param>
        /// <param name="handler">the handler that will manage the proxy</param>
        /// <returns>An instance of the implementation, casted to its implementing interface</returns>
        public object CreateNew(Type t, IProxyHandler handler, long id)
            => GetContainer(t).CreateNew(handler, id);

        public T CreateNew<T>(IProxyHandler handler, long id) where T : class
            => (T)GetContainer(typeof(T)).CreateNew(handler, id);

        public object FromContract(Type t, IProxyContract contract)
            => GetContainer(t).FromContract(contract);

        public T FromContract<T>(IProxyContract contract) where T : class
            => (T)GetContainer(typeof(T)).FromContract(contract);

        private FactoryContainer GetContainer(Type t)
        {
            lock(_containers)
            {
                if (_containers.TryGetValue(t, out FactoryContainer c))
                    return c;
                return _containers[t] = new FactoryContainer(t, _override_virtual);
            }
        }

        private readonly struct FactoryContainer
        {
            internal FactoryContainer(Type t, bool override_virtual)
            {
                TypeInfo impl = ProxyTypeCreator.CreateImplementation(t, override_virtual);

                CreateNew = CreateDelegate<Func<IProxyHandler, long, object>>(
                    impl.GetMethod(
                        ProxyTypeCreator.CreateNew,
                        BindingFlags.Static | BindingFlags.Public,
                        null,
                        new Type[] { typeof(IProxyHandler), typeof(long) },
                        null
                    )
                );

                FromContract = CreateDelegate<Func<IProxyContract, object>>(
                    impl.GetMethod(
                        ProxyTypeCreator.FromContract,
                        BindingFlags.Static | BindingFlags.Public,
                        null,
                        new Type[] { typeof(IProxyContract) },
                        null
                    )
                );


            }

            internal readonly Func<IProxyHandler, long, object> CreateNew;
            internal readonly Func<IProxyContract, object> FromContract;
        }

        private static T CreateDelegate<T>(MethodInfo mi) where T : Delegate
            => (T)mi.CreateDelegate(typeof(T));
    }
}

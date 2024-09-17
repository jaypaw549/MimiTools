using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace MimiTools.ProxyObjects
{
    public class ProxyFactory
    {
        public static ProxyFactory AbstractOnly { get; } = new ProxyFactory(false);
        public static ProxyFactory OverrideVirtual { get; } = new ProxyFactory(true);

        public static ProxyReference GetReferenceFromProxy(object proxy)
        {
            lock (UNWRAPPERS)
                if (UNWRAPPERS.TryGetValue(proxy.GetType(), out Func<object, ProxyReference> extracter))
                    return extracter(proxy);

            return null;
        }

        private static readonly ConditionalWeakTable<Type, Func<object, ProxyReference>> UNWRAPPERS = 
            new ConditionalWeakTable<Type, Func<object, ProxyReference>>();

        public ProxyFactory(bool override_virtual)
        {
            _override_virtual = override_virtual;
        }

        private readonly Dictionary<Type, FactoryContainer> _containers = new Dictionary<Type, FactoryContainer>();

        private readonly bool _override_virtual;

        public object FromReference(Type t, ProxyReference obj)
            => GetContainer(t).FromReference(obj);

        public T FromReference<T>(ProxyReference obj) where T : class
            => (T)GetContainer(typeof(T)).FromReference(obj);

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

                Func<object, ProxyReference> extracter = CreateDelegate<Func<object, ProxyReference>>(
                    impl.GetMethod(
                        ProxyTypeCreator.ExtractMethod,
                        BindingFlags.Static | BindingFlags.Public,
                        null,
                        new Type[] { typeof(object) },
                        null
                    ));

                FromReference = CreateDelegate<Func<ProxyReference, object>>(
                    impl.GetMethod(
                        ProxyTypeCreator.WrapMethod,
                        BindingFlags.Static | BindingFlags.Public,
                        null,
                        new Type[] { typeof(ProxyReference) },
                        null
                    )
                );

                lock (UNWRAPPERS)
                    UNWRAPPERS.Add(impl, extracter);
            }

            internal readonly Func<ProxyReference, object> FromReference;
        }

        private static T CreateDelegate<T>(MethodInfo mi) where T : Delegate
            => (T)mi.CreateDelegate(typeof(T));
    }
}

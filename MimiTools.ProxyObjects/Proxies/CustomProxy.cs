using MimiTools.ProxyObjects.Proxies.ProxyHandlers;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace MimiTools.ProxyObjects.Proxies
{
    public class CustomProxy<T> : IProxyHandler where T : class
    {
        private static readonly Dictionary<MethodInfo, Func<CustomProxy<T>, ProxyObject, object[], object>> _special_methods
            = new Dictionary<MethodInfo, Func<CustomProxy<T>, ProxyObject, object[], object>>();

        private static readonly WeakLazy<DynamicProxyHandler> _handler_lazy
            = new WeakLazy<DynamicProxyHandler>(() => new DynamicProxyHandler());

        static CustomProxy()
        {
            if (!typeof(T).IsInterface)
                throw new ArgumentOutOfRangeException(nameof(T));

            foreach (PropertyInfo pi in typeof(T).GetProperties())
            {
                if (pi.CanRead)
                    _special_methods.Add(pi.GetMethod, (proxy, obj, args) => proxy.PropertyGet(obj, pi, args));

                if (pi.CanWrite)
                    _special_methods.Add(pi.GetMethod, (proxy, obj, args) => proxy.PropertySet(obj, pi, args));
            }

            foreach (EventInfo ei in typeof(T).GetEvents())
            {
                _special_methods.Add(ei.AddMethod, (proxy, obj, args) => proxy.EventAdd(obj, ei, args[0] as Delegate));
                _special_methods.Add(ei.RemoveMethod, (proxy, obj, args) => proxy.EventRemove(obj, ei, args[0] as Delegate));
            }
        }

        public CustomProxy(T value)
        {
            _handler = _handler_lazy.GetValue();
            Target = value ?? throw new ArgumentNullException(nameof(value));
            _proxy = new WeakLazy<T>(CreateProxy);
        }

        public CustomProxy()
        {
            Target = null;
            _handler = null;
            _proxy = new WeakLazy<T>(CreateNullProxy);
        }

        private readonly DynamicProxyHandler _handler;
        private readonly WeakLazy<T> _proxy;

        public T Proxy { get => _proxy.GetValue(); }
        public T Target { get; }

        public event Action<T, EventInfo, Delegate> DoEventListenerAdd;
        public event Action<T, EventInfo, Delegate> AfterEventListenerAdd;
        public event Action<T, EventInfo, Delegate> OnEventListenerAdd;

        public event Action<T, EventInfo, Delegate> DoEventListenerRemove;
        public event Action<T, EventInfo, Delegate> AfterEventListenerRemove;
        public event Action<T, EventInfo, Delegate> OnEventListenerRemove;

        public event Func<T, MethodInfo, object[], object> DoMethodInvoke;
        public event Action<T, MethodInfo, object[], object> AfterMethodInvoke;
        public event Action<T, MethodInfo, object[]> OnMethodInvoke;

        public event Func<T, PropertyInfo, object[], object> DoPropertyGet;
        public event Action<T, PropertyInfo, object[], object> AfterPropertyGet;
        public event Action<T, PropertyInfo, object[]> OnPropertyGet;

        public event Action<T, PropertyInfo, object[]> DoPropertySet;
        public event Action<T, PropertyInfo, object[]> AfterPropertySet;
        public event Action<T, PropertyInfo, object[]> OnPropertySet;

        public bool CheckProxy(long id, Type contract_type)
            => _handler?.CheckProxy(id, contract_type) ?? true;

        private T CreateProxy()
            => ProxyGenerator<T>.CreateProxy(_handler.BindObject(Target), this);

        private T CreateNullProxy()
            => ProxyGenerator<T>.CreateProxy(0, this);

        private object EventAdd(ProxyObject obj, EventInfo @event, Delegate arg)
        {
            OnEventListenerAdd?.Invoke(Target, @event, arg);
            if (DoEventListenerAdd != null)
                DoEventListenerAdd(Target, @event, arg);
            else
                Invoke(obj, @event.AddMethod, arg);
            AfterEventListenerAdd?.Invoke(Target, @event, arg);
            return null;
        }

        private object EventRemove(ProxyObject obj, EventInfo @event, Delegate arg)
        {
            OnEventListenerRemove?.Invoke(Target, @event, arg);
            if (DoEventListenerRemove != null)
                DoEventListenerRemove(Target, @event, arg);
            else
                Invoke(obj, @event.RemoveMethod, arg);
            AfterEventListenerRemove?.Invoke(Target, @event, arg);
            return null;
        }

        private object Invoke(ProxyObject obj, MethodInfo method, params object[] args)
            => _handler?.Invoke(obj, method, args);

        object IProxyHandler.Invoke(ProxyObject obj, MethodInfo method, object[] args)
        {
            object ret;
            if (_special_methods.TryGetValue(method, out var handler))
                return handler.Invoke(this, obj, args);

            OnMethodInvoke?.Invoke(Target, method, args);
            if (DoMethodInvoke != null)
                ret = DoMethodInvoke(Target, method, args);
            else
                ret = Invoke(obj, method, args);
            AfterMethodInvoke?.Invoke(Target, method, args, ret);
            return ret;
        }

        private object PropertyGet(ProxyObject obj, PropertyInfo property, object[] args)
        {
            object ret;
            OnPropertyGet?.Invoke(Target, property, args);
            if (DoPropertyGet != null)
                ret = DoPropertyGet(Target, property, args);
            else
                ret = Invoke(obj, property.GetMethod, args);
            AfterPropertyGet?.Invoke(Target, property, args, ret);
            return ret;
        }

        private object PropertySet(ProxyObject obj, PropertyInfo property, object[] args)
        {
            OnPropertySet?.Invoke(Target, property, args);
            if (DoPropertySet != null)
                DoPropertySet(Target, property, args);
            else
                Invoke(obj, property.SetMethod, args);
            AfterPropertySet?.Invoke(Target, property, args);
            return null;
        }

        public void Release(long id, Type contractType)
            => _handler?.Release(id, contractType);
    }
}

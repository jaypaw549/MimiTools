using MimiTools.ProxyObjects.Proxies.Helpers;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Linq;

namespace MimiTools.ProxyObjects.Proxies
{
    public delegate object CustomProxyDelegate(CustomProxy sender, object[] args);

    public class CustomProxy : IProxyContract
    {
        public CustomProxy()
        {
            _helper = null;
            _factory = ProxyFactory.OverrideVirtual;
            _obj = null;
        }

        public CustomProxy(object obj)
        {
            _factory = ProxyFactory.OverrideVirtual;
            _obj = obj ?? throw new ArgumentNullException(nameof(obj));
            _helper = new DynamicHelper();
        }

        public CustomProxy(ProxyFactory factory)
        {
            _helper = null;
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _obj = null;
        }

        public CustomProxy(object obj, ProxyFactory factory)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _obj = obj ?? throw new ArgumentNullException(nameof(obj));
            _helper = new DynamicHelper();
        }

        private readonly DynamicHelper _helper;
        private readonly ProxyFactory _factory;
        private readonly Dictionary<MethodInfo, CustomProxyDelegate> _implementations = new Dictionary<MethodInfo, CustomProxyDelegate>();
        private readonly object _obj;

        public event Func<MethodInfo, CustomProxyDelegate> MethodResolve;

        public T AsProxy<T>() where T : class
            => _factory.FromContract<T>(this);

        public object AsProxy(Type t)
            => _factory.FromContract(t, this);

        public object Invoke(ref IProxyContract contract, MethodInfo method, object[] args)
        {
            CustomProxyDelegate func;
            bool exec;
            lock (_implementations)
                exec = _implementations.TryGetValue(method, out func);

            if (exec)
                return func(this, args);

            if (_obj != null)
                return _helper.GetMethod(method).Invoke(_obj, args);

            var resolvers = MethodResolve?.GetInvocationList();

            if (resolvers != null)
                foreach (var r in resolvers.Cast<Func<MethodInfo, CustomProxyDelegate>>())
                {
                    var cpd = r(method);
                    if (cpd != null)
                    {
                        lock (_implementations)
                            _implementations[method] = cpd;
                        return cpd(null, args);
                    }
                }

            throw new NotImplementedException();
        }

        public bool RemoveOverride(MethodInfo method)
        {
            lock (_implementations)
                return _implementations.Remove(method);
        }

        public void SetOverride(MethodInfo method, CustomProxyDelegate d)
        {
            lock (_implementations)
                _implementations[method] = d;
        }

        void IProxyContract.Release() { }

        public bool Verify(Type t)
        {
            if (_obj == null)
                return true;
            return t?.IsInstanceOfType(_obj) ?? false;
        }
    }
}

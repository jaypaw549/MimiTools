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
            _obj = null;
        }

        public CustomProxy(object obj)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj), "Parameter cannot be null! if you don't want to wrap an object, use the default constructor!");
            _obj = obj;
        }

        private readonly DynamicHelper _helper;
        private readonly Dictionary<MethodInfo, CustomProxyDelegate> _implementations = new Dictionary<MethodInfo, CustomProxyDelegate>();
        private readonly object _obj;

        public event Func<MethodInfo, CustomProxyDelegate> MethodResolve;

        public T AsProxy<T>() where T : class
            => ProxyFactory.Default.FromContract<T>(this);

        public object AsProxy<T>(Type t)
            => ProxyFactory.Default.FromContract(t, this);

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
                foreach (var cpd in from r in resolvers.Cast<Func<MethodInfo, CustomProxyDelegate>>()
                                    let cpd = r(method)
                                    where cpd != null
                                    select cpd)
                {
                    lock (_implementations)
                        _implementations[method] = cpd;
                    return cpd(this, args);
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

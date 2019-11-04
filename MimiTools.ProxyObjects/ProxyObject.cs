using System;
using System.Reflection;

namespace MimiTools.ProxyObjects
{
    public class ProxyObject
    {
        internal static readonly MethodInfo InvokeMethod = typeof(ProxyObject).GetMethod(nameof(Invoke), BindingFlags.NonPublic | BindingFlags.Instance);
        internal static readonly ConstructorInfo Constructor = typeof(ProxyObject).GetConstructors(BindingFlags.Public | BindingFlags.Instance)[0];

        internal readonly IProxyHandler _handler;

        /// <summary>
        /// The type this proxy object is representings
        /// </summary>
        public Type ContractType { get; }

        /// <summary>
        /// The Id of this proxy object. Handlers use this to identify which object they will be working with.
        /// </summary>
        public long Id { get; }

        /// <summary>
        /// Construct a proxy object, if called directly, it does not implement the contract.
        /// It is recommended you extend this class, or use ProxyGenerator to generate the proxy object
        /// if you want that behavior
        /// </summary>
        /// <param name="contract">The contract this proxy is using.</param>
        /// <param name="id">The id of the proxy object.</param>
        /// <param name="handler">The handler for this object, it is responsible for relaying execution request to the real object</param>
        public ProxyObject(Type contract, long id, IProxyHandler handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            if (!handler.CheckProxy(id, contract))
                throw new InvalidOperationException("This proxy configuration is invalid!");

            ContractType = contract;
            _handler = handler;
            Id = id;
        }

        /// <summary>
        /// Executes a method, after checking against the perms requested. Standard handler implementations require that the method be part of the contract.
        /// </summary>
        /// <param name="method">The method we're going to invoke on our real object</param>
        /// <param name="perms">The permissions to check against</param>
        /// <param name="args">The arguments to pass to the real object's method</param>
        /// <returns>the result of the real method's execution</returns>
        protected object Invoke(MethodInfo method, object[] args)
            => _handler.Invoke(this, method, args);

        /// <summary>
        /// Releases the binding on the real object, allowing it to be cleaned up.
        /// </summary>
        public void Release()
        {
            _handler.Release(Id, ContractType);
            GC.SuppressFinalize(this);
        }

        ~ProxyObject()
        {
            _handler.Release(Id, ContractType);
        }
    }
}

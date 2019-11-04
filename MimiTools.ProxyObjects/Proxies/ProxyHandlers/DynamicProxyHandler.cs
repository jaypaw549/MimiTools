using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;

namespace MimiTools.ProxyObjects.Proxies.ProxyHandlers
{
    internal class DynamicProxyHandler : IProxyHandler
    {
        private readonly Dictionary<MethodInfo, Func<object, object[], object>> _methods = new Dictionary<MethodInfo, Func<object, object[], object>>();

        private readonly Dictionary<long, object> _bindings = new Dictionary<long, object>();
        private long _current = -1;

        public long BindObject(object obj)
        {
            long id = Interlocked.Increment(ref _current);
            _bindings[id] = obj;
            return id;
        }

        private void BuildMethod(MethodInfo method, out Func<object, object[], object> invoke)
        {
            DynamicMethod dynamicMethod = new DynamicMethod(method.Name, typeof(object), new Type[] { typeof(object), typeof(object[]) });

            ILGenerator il = dynamicMethod.GetILGenerator();

            ParameterInfo[] parameters = method.GetParameters();

            Label check_fail = il.DefineLabel();

            if (!method.IsStatic)
            {
                //Sanity checks
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Isinst, method.DeclaringType);
                il.Emit(OpCodes.Dup);
                il.Emit(OpCodes.Brfalse_S, check_fail);
                if (method.DeclaringType.IsValueType)
                    il.Emit(OpCodes.Unbox, method.DeclaringType);
            }

            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldlen);
            il.Emit(OpCodes.Ldc_I4, parameters.Length);
            il.Emit(OpCodes.Ceq);
            il.Emit(OpCodes.Brfalse_S, check_fail);

            LocalBuilder[] out_holders = new LocalBuilder[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                Type type = parameters[i].ParameterType;

                bool ref_type = type.IsByRef;
                if (ref_type)
                    type = type.GetElementType();

                if (ref_type && parameters[i].IsOut)
                {
                    out_holders[i] = il.DeclareLocal(type);
                    il.Emit(OpCodes.Ldloca_S, out_holders[i]);
                    continue;
                }

                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldc_I4, i);
                il.Emit(OpCodes.Ldelem, typeof(object));

                if (type.IsValueType)
                {
                    il.Emit(OpCodes.Unbox, type);
                    il.Emit(OpCodes.Ldobj, type);
                }
                else
                    il.Emit(OpCodes.Castclass, type);

                if (ref_type)
                {
                    LocalBuilder storage = il.DeclareLocal(type);

                    if (!parameters[i].IsIn)
                        out_holders[i] = storage;

                    il.Emit(OpCodes.Stloc_S, storage);
                    il.Emit(OpCodes.Ldloca_S, storage);
                }
            }

            il.Emit(OpCodes.Callvirt, method);

            if (method.ReturnType == typeof(void))
                il.Emit(OpCodes.Ldnull);
            else if (method.ReturnType.IsValueType)
                il.Emit(OpCodes.Box, method.ReturnType);

            for (int i = 0; i < out_holders.Length; i++)
            {
                LocalBuilder holder = out_holders[i];
                if (holder != null)
                {
                    Type type = parameters[i].ParameterType.GetElementType();
                    il.Emit(OpCodes.Ldarg_1);
                    il.Emit(OpCodes.Ldc_I4, i);
                    il.Emit(OpCodes.Ldloc, holder);
                    if (type.IsValueType)
                        il.Emit(OpCodes.Box, type);
                    il.Emit(OpCodes.Stelem, typeof(object));
                }
            }

            il.Emit(OpCodes.Ret);
            il.MarkLabel(check_fail);
            il.ThrowException(typeof(ArgumentException));

            _methods[method] = invoke = (Func<object, object[], object>)dynamicMethod.CreateDelegate(typeof(Func<object, object[], object>));
            return;
        }

        public object Invoke(ProxyObject obj, MethodInfo method, object[] args)
        {
            Func<object, object[], object> invoke;
            lock (_methods)
            {
                if (!_methods.TryGetValue(method, out invoke))
                    BuildMethod(method, out invoke);
            }

            return invoke(_bindings[obj.Id], args);
        }

        public bool CheckProxy(long id, Type contract_type)
        {
            if (_bindings.TryGetValue(id, out object value))
                return contract_type.IsInstanceOfType(value);
            return false;
        }

        public void Release(long id, Type contractType)
            => _bindings.Remove(id);
    }
}

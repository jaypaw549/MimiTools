using MimiTools.Collections.Weak;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using static MimiTools.ProxyObjects.ProxyHelper;

namespace MimiTools.ProxyObjects
{
    public static class ProxyGenerator
    {
        private static readonly WeakDictionary<Type, Func<long, IProxyHandler, ProxyObject>> _builders
            = new WeakDictionary<Type, Func<long, IProxyHandler, ProxyObject>>();

        private static readonly MethodInfo _create_object = typeof(ProxyGenerator).GetMethod(nameof(CreateObject), BindingFlags.Static | BindingFlags.NonPublic);

        /// <summary>
        /// Creates a "transparent" proxy of the specified type, returning it as a ProxyObject
        /// </summary>
        /// <param name="t">The type to make a transparent proxy of</param>
        /// <param name="id">The id to pass the proxy</param>
        /// <param name="handler">The handler that will manage the proxy</param>
        /// <returns>A proxy object implementing the specified interface</returns>
        public static ProxyObject CreateProxy(Type t, long id, IProxyHandler handler)
        {
            if (!_builders.TryGetValue(t, out var builder))
                _builders[t] = builder = (Func<long, IProxyHandler, ProxyObject>)
                    _create_object.MakeGenericMethod(t)
                    .CreateDelegate(typeof(Func<long, IProxyHandler, ProxyObject>));

            return builder.Invoke(id, handler);
        }

        /// <summary>
        /// Creates a transparent proxy of the specified type, returning it as a ProxyObject
        /// </summary>
        /// <typeparam name="T">The type to make a transparent proxy of</param>
        /// <param name="id">The id to pass the proxy</param>
        /// <param name="handler">The handler that will manage the proxy</param>
        /// <returns>A proxy object implementing the specified interface</returns>
        public static T CreateProxy<T>(long id, IProxyHandler handler) where T : class
            => ProxyGenerator<T>.CreateProxy(id, handler);

        private static ProxyObject CreateObject<T>(long id, IProxyHandler handler) where T : class
            => (ProxyObject)(object)ProxyGenerator<T>.CreateProxy(id, handler);
    }

    public static class ProxyGenerator<T> where T : class
    {
        private const string _constructor_delegate_name = "CreateInstance";

        private static readonly WeakLazy<Func<long, IProxyHandler, T>> _ctor = new WeakLazy<Func<long, IProxyHandler, T>>(() =>
        {
            MethodInfo mi = _impl.GetValue().GetMethod(
                _constructor_delegate_name,
                BindingFlags.Static | BindingFlags.Public,
                null,
                new Type[] { typeof(long), typeof(IProxyHandler) }, null);

            return (Func<long, IProxyHandler, T>)mi.CreateDelegate(typeof(Func<long, IProxyHandler, T>));
        });
        private static readonly WeakLazy<TypeInfo> _impl = new WeakLazy<TypeInfo>(() => CreateImplementation(typeof(T)));

        /// <summary>
        /// A delegate that constructs proxies of this type. If you're looking to just wrap objects, use DynamicProxy or ReflectionProxy
        /// </summary>
        public static Func<long, IProxyHandler, T> ConstructorDelegate => _ctor.GetValue();

        /// <summary>
        /// The type generated to create the transparent proxy. The type implements the interface specified, and extends ProxyObject.
        /// </summary>
        public static TypeInfo Implementation => _impl.GetValue();

        /// <summary>
        /// Create a transparent proxy using the specified id, permissions, and handler.
        /// </summary>
        /// <param name="id">The id to make the proxy with</param>
        /// <param name="handler">the handler that will manage the proxy</param>
        /// <returns>An instance of the implementation, casted to its implementing interface</returns>
        public static T CreateProxy(long id, IProxyHandler handler)
            => _ctor.GetValue()(id, handler);

        //private static CustomAttributeBuilder CopyCustomAttribute(CustomAttributeData data)
        //{
        //    CustomAttributeTypedArgument[] attr_c_args = data.ConstructorArguments.ToArray();
        //    CustomAttributeNamedArgument[] attr_f_args = data.NamedArguments.Where(arg => arg.IsField).ToArray();
        //    CustomAttributeNamedArgument[] attr_p_args = data.NamedArguments.Where(arg => !arg.IsField).ToArray();

        //    return new CustomAttributeBuilder(
        //        data.Constructor,
        //        Array.ConvertAll(attr_c_args, arg => arg.Value),
        //        Array.ConvertAll(attr_p_args, arg => (PropertyInfo)arg.MemberInfo),
        //        Array.ConvertAll(attr_p_args, arg => arg.TypedValue.Value),
        //        Array.ConvertAll(attr_f_args, arg => (FieldInfo)arg.MemberInfo),
        //        Array.ConvertAll(attr_f_args, arg => arg.TypedValue.Value)
        //        );
        //}

        private static MethodBuilder CreateConstructorDelegate(TypeBuilder typeBuilder, ConstructorBuilder constructorBuilder)
        {
            int count = ProxyObject.Constructor.GetParameters().Length - 1;
            MethodBuilder methodBuilder = typeBuilder.DefineMethod(_constructor_delegate_name, MethodAttributes.Public | MethodAttributes.Static,
                typeof(T), ProxyObject.Constructor.GetParameters().Skip(1).Select(p => p.ParameterType).ToArray());

            ILGenerator generator = methodBuilder.GetILGenerator();
            for (int i = 0; i < count; i++)
                generator.Emit(OpCodes.Ldarg_S, i);

            generator.Emit(OpCodes.Newobj, constructorBuilder);
            generator.Emit(OpCodes.Castclass, typeof(T));
            generator.Emit(OpCodes.Ret);

            return methodBuilder;
        }

        private static TypeInfo CreateImplementation(Type @interface)
        {
            if (!@interface.IsInterface)
                throw new NotSupportedException("Cannot generate types for non-interfaces! Please extend ProxyObject manually, or provide an interface type instead!");

            AssemblyBuilder asmBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName($"ProxyObject.{@interface.Name}.dll"), AssemblyBuilderAccess.RunAndCollect);
            ModuleBuilder modBuilder = asmBuilder.DefineDynamicModule(@interface.FullName);
            TypeBuilder typeBuilder = modBuilder.DefineType(
                "Proxy" + @interface.Name,
                TypeAttributes.Public | TypeAttributes.Serializable | TypeAttributes.Class,
                typeof(ProxyObject));

            CreateConstructorDelegate(typeBuilder, ImplementConstructor(typeBuilder));

            typeBuilder.AddInterfaceImplementation(@interface);

            HashSet<MethodInfo> implemented = new HashSet<MethodInfo>();

            Implement(@interface, typeBuilder, implemented);
            foreach (Type i in @interface.GetInterfaces())
                Implement(i, typeBuilder, implemented);

            return typeBuilder.CreateTypeInfo();
        }

        private static void EmitUnbox(ILGenerator generator, Type t)
        {
            if (t is GenericTypeParameterBuilder)
            {
                generator.Emit(OpCodes.Unbox_Any, t);
            }
            else if (t.IsValueType)
            {
                generator.Emit(OpCodes.Unbox, t);
                generator.Emit(OpCodes.Ldobj, t);
            }
            else
                generator.Emit(OpCodes.Castclass, t);
        }

        private static void Implement(Type @interface, TypeBuilder typeBuilder, HashSet<MethodInfo> implemented)
        {
            //Special cases
            foreach (PropertyInfo pi in @interface.GetProperties())
            {
                if (pi.GetMethod != null)
                {
                    if (implemented.Add(pi.GetMethod))
                        ImplementMethod(typeBuilder, pi.GetMethod);
                }

                if (pi.SetMethod != null)
                {
                    if (implemented.Add(pi.SetMethod))
                        ImplementMethod(typeBuilder, pi.SetMethod);
                }
            }

            //Special cases
            foreach (EventInfo ei in @interface.GetEvents())
            {
                if (ei.AddMethod != null)
                {
                    if (implemented.Add(ei.AddMethod))
                        ImplementMethod(typeBuilder, ei.AddMethod);
                }

                if (ei.RemoveMethod != null)
                {
                    if (implemented.Add(ei.RemoveMethod))
                        ImplementMethod(typeBuilder, ei.RemoveMethod);
                }
            }

            //General cases
            foreach (MethodInfo mi in @interface.GetMethods())
                if (implemented.Add(mi))
                    ImplementMethod(typeBuilder, mi);
        }

        private static ConstructorBuilder ImplementConstructor(TypeBuilder typeBuilder)
        {
            int count = ProxyObject.Constructor.GetParameters().Length - 1;
            ConstructorBuilder constructorBuilder = typeBuilder.DefineConstructor(
                MethodAttributes.Public,
                ProxyObject.Constructor.CallingConvention,
                ProxyObject.Constructor.GetParameters().Skip(1).Select(p => p.ParameterType).ToArray()
                );

            ILGenerator generator = constructorBuilder.GetILGenerator();

            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldtoken, typeof(T));
            generator.Emit(OpCodes.Call, TypeOfOperation);

            for (int i = 1; i <= count; i++)
                generator.Emit(OpCodes.Ldarg_S, i);

            generator.Emit(OpCodes.Call, ProxyObject.Constructor);
            generator.Emit(OpCodes.Ret);

            return constructorBuilder;
        }

        private static MethodBuilder ImplementMethod(TypeBuilder typeBuilder, MethodInfo mi)
        {
            MethodBuilder methodBuilder;

            if (mi.IsGenericMethodDefinition)
                methodBuilder = ImplementMethodGeneric(typeBuilder, mi);
            else
                methodBuilder = ImplementMethodStandard(typeBuilder, mi);

            typeBuilder.DefineMethodOverride(methodBuilder, mi);

            return methodBuilder;
        }

        private static void ImplementMethodCode(MethodBuilder methodBuilder, MethodInfo mi, GenericTypeParameterBuilder[] gen_parameters, Type[] parameters, Type ret_type)
        {
            methodBuilder.SetSignature(
                ret_type,
                mi.ReturnParameter.GetRequiredCustomModifiers(),
                mi.ReturnParameter.GetOptionalCustomModifiers(),
                parameters,
                Array.ConvertAll(mi.GetParameters(), pi => pi.GetRequiredCustomModifiers()),
                Array.ConvertAll(mi.GetParameters(), pi => pi.GetOptionalCustomModifiers()));

            ILGenerator generator = methodBuilder.GetILGenerator();

            //Instance object
            generator.Emit(OpCodes.Ldarg_0);

            //First argument, The method
            if (gen_parameters != null)
                generator.Emit(OpCodes.Ldtoken, mi.MakeGenericMethod(gen_parameters));
            else
                generator.Emit(OpCodes.Ldtoken, mi);

            generator.Emit(OpCodes.Ldtoken, typeof(T));
            generator.Emit(OpCodes.Call, GetMethodOperation);
            generator.Emit(OpCodes.Isinst, typeof(MethodInfo));

            //Second argument, the method arguments.
            LocalBuilder args_array = generator.DeclareLocal(typeof(object[]));
            generator.Emit(OpCodes.Ldc_I4, parameters.Length);
            generator.Emit(OpCodes.Newarr, typeof(object));
            generator.Emit(OpCodes.Dup);
            generator.Emit(OpCodes.Stloc_S, args_array);

            ParameterInfo[] para_infos = mi.GetParameters();
            bool[] is_out = new bool[para_infos.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                ParameterInfo pi = para_infos[i];
                Type p = parameters[i];
                methodBuilder.DefineParameter(pi.Position, pi.Attributes, pi.Name);

                //foreach (CustomAttributeData data in pi.CustomAttributes)
                //    parameterBuilder.SetCustomAttribute(CopyCustomAttribute(data));

                generator.Emit(OpCodes.Dup);
                generator.Emit(OpCodes.Ldc_I4, i);
                generator.Emit(OpCodes.Ldarg_S, i + 1);

                if (p.IsByRef)
                {
                    p = p.GetElementType();
                    if (pi.IsOut)
                    {
                        generator.Emit(OpCodes.Dup);
                        generator.Emit(OpCodes.Initobj, p);
                    }
                    generator.Emit(OpCodes.Ldobj, p);

                    is_out[i] = !pi.IsIn;
                }
                else
                    is_out[i] = false;

                if (p.IsValueType || p.IsGenericParameter)
                    generator.Emit(OpCodes.Box, p);

                generator.Emit(OpCodes.Stelem, typeof(object));
            }

            generator.Emit(OpCodes.Call, ProxyObject.InvokeMethod);

            if (ret_type == typeof(void))
                generator.Emit(OpCodes.Pop);
            else
                EmitUnbox(generator, ret_type);

            for (int i = 0; i < is_out.Length; i++)
                if (is_out[i])
                {
                    generator.Emit(OpCodes.Ldarg_S, i + 1);
                    generator.Emit(OpCodes.Ldloc, args_array);
                    generator.Emit(OpCodes.Ldc_I4, i);
                    generator.Emit(OpCodes.Ldelem, typeof(object));
                    EmitUnbox(generator, parameters[i].GetElementType());
                    generator.Emit(OpCodes.Stind_Ref);
                }

            generator.Emit(OpCodes.Ret);
        }

        private static MethodBuilder ImplementMethodGeneric(TypeBuilder typeBuilder, MethodInfo mi)
        {
            Type return_type = mi.ReturnType;
            Type[] parameters = mi.GetParameters().Select(p => p.ParameterType).ToArray();
            Type[] generic_parameters = mi.GetGenericArguments();

            MethodBuilder methodBuilder = typeBuilder.DefineMethod(
                mi.Name,
                mi.Attributes & ~MethodAttributes.Abstract,
                mi.CallingConvention
                );

            GenericTypeParameterBuilder[] genericBuilders = methodBuilder.DefineGenericParameters(generic_parameters.Select(p => p.Name).ToArray());
            Dictionary<Type, GenericTypeParameterBuilder> map = new Dictionary<Type, GenericTypeParameterBuilder>();

            for (int i = 0; i < genericBuilders.Length; i++)
            {
                GenericTypeParameterBuilder genericBuilder = genericBuilders[i];
                Type genericType = generic_parameters[i];

                map.Add(genericType, genericBuilder);

                genericBuilder.SetGenericParameterAttributes(genericType.GenericParameterAttributes);

                if (genericType.BaseType != null)
                {
                    genericBuilder.SetBaseTypeConstraint(genericType.BaseType);
                }
                else
                {
                    genericBuilder.SetInterfaceConstraints(genericType.GetInterfaces());
                }
            }

            RecursiveReplaceAll(parameters, map);
            RecursiveReplace(ref return_type, map);

            ImplementMethodCode(methodBuilder, mi, genericBuilders, parameters, return_type);

            return methodBuilder;
        }

        private static MethodBuilder ImplementMethodStandard(TypeBuilder typeBuilder, MethodInfo mi)
        {
            Type[] parameters = Array.ConvertAll(mi.GetParameters(), p => p.ParameterType);

            MethodBuilder methodBuilder = typeBuilder.DefineMethod(
                mi.Name,
                mi.Attributes & ~MethodAttributes.Abstract,
                mi.CallingConvention
                );

            ImplementMethodCode(methodBuilder, mi, null, parameters, mi.ReturnType);

            return methodBuilder;
        }

        private static bool RecursiveReplace(ref Type type, Dictionary<Type, GenericTypeParameterBuilder> map)
        {
            if (map.TryGetValue(type, out GenericTypeParameterBuilder gen_type))
            {
                type = gen_type;
                return true;
            }

            if (type.HasElementType)
            {
                Type elementType = type.GetElementType();
                if (RecursiveReplace(ref elementType, map))
                {
                    if (type.IsArray)
                        type = elementType.MakeArrayType();
                    else if (type.IsByRef)
                        type = elementType.MakeByRefType();
                    else if (type.IsPointer)
                        type = elementType.MakePointerType();
                    else
                        throw new InvalidOperationException("Cannot create the specified type!");

                    return true;
                }
            }

            if (type.IsGenericType)
            {
                Type[] parameters = type.GetGenericArguments();
                if (RecursiveReplaceAll(parameters, map))
                {
                    type = type.GetGenericTypeDefinition().MakeGenericType(parameters);
                    return true;
                }
            }

            return false;
        }

        private static bool RecursiveReplaceAll(Type[] parameters, Dictionary<Type, GenericTypeParameterBuilder> map)
        {
            bool changed = false;

            for (int i = 0; i < parameters.Length; i++)
                changed |= RecursiveReplace(ref parameters[i], map);

            return changed;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace MimiTools.ProxyObjects
{
    internal static class ProxyTypeCreator
    {
        internal const string CreateNew = "CreateNew";
        internal const string FromContract = "FromContract";

        private const BindingFlags _flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        internal static TypeInfo CreateImplementation(Type type, bool virt)
        {
            if (!type.IsPublic || !type.IsNestedPublic)
                throw new ArgumentException("Generated assemblies cannot access the target class!");

            AssemblyBuilder asmBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName($"ProxyObject.{type.Name}.dll"), AssemblyBuilderAccess.RunAndCollect);
            ModuleBuilder modBuilder = asmBuilder.DefineDynamicModule(type.FullName);
            TypeBuilder typeBuilder = modBuilder.DefineType(
                "Proxy" + type.Name,
                TypeAttributes.Public | TypeAttributes.Serializable | TypeAttributes.Class);

            FieldBuilder fieldContract = typeBuilder.DefineField("_contract", typeof(IProxyContract), FieldAttributes.Private | FieldAttributes.InitOnly);

            CreateNewDelegate(type, typeBuilder, ImplementNewConstructor(type, typeBuilder, fieldContract));
            CreateWrapperDelegate(type, typeBuilder, ImplementWrapperConstructor(type, typeBuilder, fieldContract));

            if (type.IsInterface)
            {
                typeBuilder.SetParent(typeof(ValueType));
                typeBuilder.AddInterfaceImplementation(type);
            }

            else if (type.IsClass && !type.IsSealed)
                typeBuilder.SetParent(type);

            else
                throw new ArgumentException("Cannot extend the specified type!");

            HashSet<MethodInfo> implemented = new HashSet<MethodInfo>();

            Implement(type, typeBuilder, fieldContract, virt, implemented);

            if (type.IsInterface)
                foreach (Type i in type.GetInterfaces())
                    Implement(i, typeBuilder, fieldContract, virt, implemented);

            return typeBuilder.CreateTypeInfo();
        }

        private static MethodBuilder CreateNewDelegate(Type type, TypeBuilder typeBuilder, ConstructorBuilder constructorBuilder)
        {
            MethodBuilder methodBuilder = typeBuilder.DefineMethod(CreateNew, MethodAttributes.Public | MethodAttributes.Static,
                type, new Type[] { typeof(IProxyHandler), typeof(long) });

            ILGenerator generator = methodBuilder.GetILGenerator();

            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldarg_1);
            generator.Emit(OpCodes.Newobj, constructorBuilder);
            generator.Emit(OpCodes.Castclass, type);
            generator.Emit(OpCodes.Ret);

            return methodBuilder;
        }

        private static MethodBuilder CreateWrapperDelegate(Type type, TypeBuilder typeBuilder, ConstructorBuilder constructorBuilder)
        {
            MethodBuilder methodBuilder = typeBuilder.DefineMethod(FromContract, MethodAttributes.Public | MethodAttributes.Static,
                type, new Type[] { typeof(IProxyContract) });

            ILGenerator generator = methodBuilder.GetILGenerator();

            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Newobj, constructorBuilder);
            generator.Emit(OpCodes.Castclass, type);
            generator.Emit(OpCodes.Ret);

            return methodBuilder;
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

        private static void Implement(Type type, TypeBuilder typeBuilder, FieldBuilder fieldContract, bool virt, HashSet<MethodInfo> implemented)
        {
            //Special cases
            foreach (PropertyInfo pi in type.GetProperties(_flags))
            {
                if (pi.GetMethod != null)
                {
                    if (implemented.Add(pi.GetMethod))
                        ImplementMethod(type, typeBuilder, fieldContract, pi.GetMethod, virt);
                }

                if (pi.SetMethod != null)
                {
                    if (implemented.Add(pi.SetMethod))
                        ImplementMethod(type, typeBuilder, fieldContract, pi.SetMethod, virt);
                }
            }

            //Special cases
            foreach (EventInfo ei in type.GetEvents(_flags))
            {
                if (ei.AddMethod != null)
                {
                    if (implemented.Add(ei.AddMethod))
                        ImplementMethod(type, typeBuilder, fieldContract, ei.AddMethod, virt);
                }

                if (ei.RemoveMethod != null)
                {
                    if (implemented.Add(ei.RemoveMethod))
                        ImplementMethod(type, typeBuilder, fieldContract, ei.RemoveMethod, virt);
                }

                if (ei.RaiseMethod != null)
                    if (implemented.Add(ei.RaiseMethod))
                        ImplementMethod(type, typeBuilder, fieldContract, ei.RaiseMethod, virt);
            }

            //General cases
            foreach (MethodInfo mi in type.GetMethods(_flags))
                if (implemented.Add(mi))
                    ImplementMethod(type, typeBuilder, fieldContract, mi, virt);
        }

        private static MethodBuilder ImplementMethod(Type type, TypeBuilder typeBuilder, FieldBuilder fieldContract, MethodInfo mi, bool virt)
        {
            if (!mi.IsPublic && !mi.IsFamily && !mi.IsFamilyOrAssembly)
            {
                if (mi.IsAbstract)
                    throw new InvalidOperationException("Cannot access non-public and non-family members to override them!");
                return null;
            }

            if (!mi.IsAbstract && !virt)
                return null;

            MethodBuilder methodBuilder;

            if (mi.IsGenericMethodDefinition)
                methodBuilder = ImplementMethodGeneric(type, typeBuilder, fieldContract, mi);
            else
                methodBuilder = ImplementMethodStandard(type, typeBuilder, fieldContract, mi);

            typeBuilder.DefineMethodOverride(methodBuilder, mi);

            return methodBuilder;
        }

        private static void ImplementMethodCode(Type type, MethodBuilder methodBuilder, FieldBuilder fieldContract, MethodInfo mi, GenericTypeParameterBuilder[] gen_parameters, Type[] parameters, Type ret_type)
        {
            methodBuilder.SetSignature(
                ret_type,
                mi.ReturnParameter.GetRequiredCustomModifiers(),
                mi.ReturnParameter.GetOptionalCustomModifiers(),
                parameters,
                Array.ConvertAll(mi.GetParameters(), pi => pi.GetRequiredCustomModifiers()),
                Array.ConvertAll(mi.GetParameters(), pi => pi.GetOptionalCustomModifiers()));

            ILGenerator generator = methodBuilder.GetILGenerator();

            //Load Instance object
            generator.Emit(OpCodes.Ldarg_0);

            //Load contract object
            generator.Emit(OpCodes.Ldfld, fieldContract);

            //Load contract reference
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldflda, fieldContract);

            //First argument, The method
            if (gen_parameters != null)
                generator.Emit(OpCodes.Ldtoken, mi.MakeGenericMethod(gen_parameters));
            else
                generator.Emit(OpCodes.Ldtoken, mi);

            generator.Emit(OpCodes.Ldtoken, type);
            generator.Emit(OpCodes.Call, ProxyHelper.GetMethodOperation);
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

            generator.Emit(OpCodes.Callvirt, ProxyHelper.InvokeMethod);

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

        private static MethodBuilder ImplementMethodGeneric(Type type, TypeBuilder typeBuilder, FieldBuilder fieldContract, MethodInfo mi)
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

            ImplementMethodCode(type, methodBuilder, fieldContract, mi, genericBuilders, parameters, return_type);

            return methodBuilder;
        }

        private static MethodBuilder ImplementMethodStandard(Type type, TypeBuilder typeBuilder, FieldBuilder fieldContract, MethodInfo mi)
        {
            Type[] parameters = Array.ConvertAll(mi.GetParameters(), p => p.ParameterType);

            MethodBuilder methodBuilder = typeBuilder.DefineMethod(
                mi.Name,
                mi.Attributes & ~MethodAttributes.Abstract,
                mi.CallingConvention
                );

            ImplementMethodCode(type, methodBuilder, fieldContract, mi, null, parameters, mi.ReturnType);

            return methodBuilder;
        }

        private static ConstructorBuilder ImplementNewConstructor(Type type, TypeBuilder typeBuilder, FieldBuilder contractField)
        {
            ConstructorBuilder constructorBuilder = typeBuilder.DefineConstructor(
                MethodAttributes.Public,
                CallingConventions.HasThis,
                new Type[] { typeof(IProxyHandler), typeof(long) }
                );

            ILGenerator generator = constructorBuilder.GetILGenerator();

            if (type.IsClass)
            {
                ConstructorInfo constructor = type.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);

                if (constructor == null)
                    throw new InvalidOperationException("No default constructor detected!");

                if (!constructor.IsPublic | !constructor.IsFamily | !constructor.IsFamilyOrAssembly)
                    throw new InvalidOperationException("Default constructor is inaccessible!");

                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Call, constructor);
            }

            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldarg_1);
            generator.Emit(OpCodes.Ldarg_2);
            generator.Emit(OpCodes.Ldtoken, type);
            generator.Emit(OpCodes.Call, ProxyHelper.TypeOfOperation);
            generator.Emit(OpCodes.Callvirt, ProxyHelper.CreateContractMethod);
            generator.Emit(OpCodes.Stfld, contractField);
            generator.Emit(OpCodes.Ret);

            return constructorBuilder;
        }

        private static ConstructorBuilder ImplementWrapperConstructor(Type type, TypeBuilder typeBuilder, FieldBuilder contractField)
        {
            ConstructorBuilder constructorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.HasThis, new Type[] { typeof(IProxyContract) });
            ILGenerator generator = constructorBuilder.GetILGenerator();

            if (type.IsClass)
            {
                ConstructorInfo constructor = type.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);

                if (constructor == null)
                    throw new InvalidOperationException("No default constructor detected!");

                if (!constructor.IsPublic | !constructor.IsFamily | !constructor.IsFamilyOrAssembly)
                    throw new InvalidOperationException("Default constructor is inaccessible!");

                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Call, constructor);
            }

            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldarg_1);
            generator.Emit(OpCodes.Stfld, contractField);
            generator.Emit(OpCodes.Ret);

            return constructorBuilder;
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

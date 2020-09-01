using MimiTools.Memory;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace MimiTools.Implementer
{
    public static class ImplementationFactory
    {
        private static int factory_id = 0;

        private const string CreateInstanceMethod = "CreateInstance";

        public static TDelegate CreateFactory<TDelegate>(IImplementationProvider impl_provider) where TDelegate : Delegate
        {
            SetupToolkit(typeof(TDelegate), impl_provider, out ImplToolkit toolkit);
            BuildConstructor(ref toolkit);
            BuildMethods(ref toolkit);

            return (TDelegate) toolkit.Type.CreateTypeInfo().GetMethod(CreateInstanceMethod, BindingFlags.Public | BindingFlags.Static).CreateDelegate(toolkit.FactoryDelegateType);
        }

        private static void BuildConstructor(ref ImplToolkit toolkit)
        {
            ConstructorBuilder c_builder = toolkit.Type.DefineConstructor(
                MethodAttributes.Public,
                CallingConventions.HasThis,
                toolkit.ParametersData.m_parameterTypes,
                toolkit.ParametersData.m_requiredModifiers,
                toolkit.ParametersData.m_optionalModifiers);

            ILGenerator il = c_builder.GetILGenerator();

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, typeof(object).GetConstructor(Type.EmptyTypes));

            FieldDefinition[] field_defs = toolkit.Provider.GetFieldDefinitions(toolkit.BaseType, in toolkit.ParametersData);
            toolkit.Fields = new FieldBuilder[field_defs.Length];

            for(int i = 0; i < field_defs.Length; i++)
            {
                ref FieldDefinition def = ref field_defs[i];
                toolkit.Fields[i] = toolkit.Type.DefineField(def.Name, def.Type, def.RequiredCustomModifiers, def.OptionalCustomModifiers, def.FieldAttributes);

                FieldInitConfig setter = new FieldInitConfig(ref Unsafe.AsRef(in def), toolkit.Fields[i], il, ref toolkit, out bool isSet);
                toolkit.Provider.SetField(toolkit.BaseType, in setter);

                if (!isSet)
                    setter.DoNotSet();
            }

            il.Emit(OpCodes.Ret);

            MethodBuilder create = toolkit.Type.DefineMethod(
                CreateInstanceMethod,
                MethodAttributes.Public | MethodAttributes.Static,
                CallingConventions.Standard,
                toolkit.BaseType,
                Type.EmptyTypes,
                Type.EmptyTypes,
                toolkit.ParametersData.m_parameterTypes,
                toolkit.ParametersData.m_requiredModifiers,
                toolkit.ParametersData.m_optionalModifiers);

            il = create.GetILGenerator();
            for (int i = 0; i < toolkit.ParametersData.m_parameterTypes.Length; i++)
                il.Emit(OpCodes.Ldarg, i);

            il.Emit(OpCodes.Newobj, c_builder);
            il.Emit(OpCodes.Castclass, toolkit.BaseType);
            il.Emit(OpCodes.Ret);
        }

        private static void BuildMethods(ref ImplToolkit toolkit)
        {
            MethodInfo[] i_methods = ExtractMethods(ref toolkit);
            toolkit.Methods = new MethodBuilder[i_methods.Length];
            for(int i = 0; i < i_methods.Length; i++)
            {
                ExtractMethodSignature(i_methods[i], out MethodParameters data, out Type return_type);

                toolkit.Methods[i] = toolkit.Type.DefineMethod(
                    i_methods[i].Name,
                    i_methods[i].Attributes & ~MethodAttributes.Abstract,
                    CallingConventions.HasThis);

                PrepareGenericParameters(ref toolkit, ref data, ref return_type, i_methods[i], toolkit.Methods[i]);

                toolkit.Methods[i].SetSignature(
                    return_type,
                    i_methods[i].ReturnParameter.GetRequiredCustomModifiers(),
                    i_methods[i].ReturnParameter.GetOptionalCustomModifiers(),
                    data.m_parameterTypes,
                    data.m_requiredModifiers,
                    data.m_optionalModifiers);

                toolkit.Type.DefineMethodOverride(toolkit.Methods[i], i_methods[i]);

                toolkit.Provider.WriteMethod(Array.AsReadOnly(toolkit.Fields), i_methods[i], null, in data, toolkit.Methods[i]);
            }
        }

        private static MethodInfo[] ExtractMethods(ref ImplToolkit toolkit)
        {
            toolkit.Type.AddInterfaceImplementation(toolkit.BaseType);
            HashSet<MethodInfo> found_methods = new HashSet<MethodInfo>();

            ExtractMethods(toolkit.BaseType, found_methods);
            Type[] extra = toolkit.Provider.GetAdditionalInterfaces(toolkit.BaseType, in toolkit.ParametersData);

            for (int i = 0; i < extra.Length; i++)
            {
                toolkit.Type.AddInterfaceImplementation(extra[i]);
                ExtractMethods(extra[i], found_methods);
            }

            MethodInfo[] array = new MethodInfo[found_methods.Count];
            found_methods.CopyTo(array);
            return array;
        }

        private static void ExtractMethods(Type base_type, HashSet<MethodInfo> found_methods)
        {
            MethodInfo[] array = base_type.GetMethods();
            for (int i = 0; i < array.Length; i++)
                found_methods.Add(array[i]);

            Type[] interfaces = base_type.GetInterfaces();
            for (int i = 0; i < interfaces.Length; i++)
                ExtractMethods(interfaces[i], found_methods);
        }

        private static void ExtractDelegateSignature(Type t_delegate, out MethodParameters data, out Type return_type)
            => ExtractMethodSignature(t_delegate.GetMethod("Invoke"), out data, out return_type);

        private static void ExtractMethodSignature(MethodInfo method, out MethodParameters data, out Type return_type)
        {
            ParameterInfo[] info = method.GetParameters();

            Type[] parameter_types = new Type[info.Length];
            Type[][] required_modifiers = new Type[info.Length][];
            Type[][] optional_modifiers = new Type[info.Length][];

            for (int i = 0; i < info.Length; i++)
            {
                parameter_types[i] = info[i].ParameterType;
                required_modifiers[i] = info[i].GetRequiredCustomModifiers();
                optional_modifiers[i] = info[i].GetOptionalCustomModifiers();
            }

            data = new MethodParameters(parameter_types, required_modifiers, optional_modifiers);
            return_type = method.ReturnType;
        }

        private static void PrepareGenericParameters(ref ImplToolkit toolkit, ref MethodParameters data, ref Type returnType, MethodInfo methodInfo, MethodBuilder methodBuilder)
        {
            Type[] generic_args = methodInfo.GetGenericArguments();
            if (generic_args.Length == 0)
                return;

            GenericTypeParameterBuilder[] generic_builders = methodBuilder.DefineGenericParameters(Array.ConvertAll(generic_args, arg => arg.Name));
            Dictionary<Type, GenericTypeParameterBuilder> map = new Dictionary<Type, GenericTypeParameterBuilder>();

            for(int i = 0; i < generic_args.Length; i++)
            {
                map.Add(generic_args[i], generic_builders[i]);

                if (generic_args[i].BaseType != null)
                {
                    generic_builders[i].SetBaseTypeConstraint(generic_args[i].BaseType);
                }
                else
                {
                    generic_builders[i].SetInterfaceConstraints(generic_args[i].GetInterfaces());
                }
            }

            RecursiveReplace(ref returnType, map);
            RecursiveReplaceAll(data.m_parameterTypes, map);
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

        private static void SetupToolkit(Type t_delegate, IImplementationProvider impl_provider, out ImplToolkit toolkit)
        {
            toolkit = new ImplToolkit
            {
                FactoryDelegateType = t_delegate,
                Provider = impl_provider
            };

            ExtractDelegateSignature(t_delegate, out toolkit.ParametersData, out toolkit.BaseType);

            if (!toolkit.BaseType.IsInterface)
                throw new ArgumentOutOfRangeException("TDelegate", "This only supports delegates that return interfaces!");

            toolkit.Assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName($"Factory-{Interlocked.Increment(ref factory_id)}.dll"), AssemblyBuilderAccess.RunAndCollect);
            toolkit.Module = toolkit.Assembly.DefineDynamicModule("Module");
            toolkit.Type = toolkit.Module.DefineType("Impl", TypeAttributes.Public | TypeAttributes.Sealed);
        }
    }
}

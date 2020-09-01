using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace MimiTools.Implementer
{
    public class TypeWrapperFactory : IImplementationProvider
    {
        private static readonly TypeWrapperFactory s_instance = new TypeWrapperFactory();

        private TypeWrapperFactory() { }

        Type[] IImplementationProvider.GetAdditionalInterfaces(Type baseType, in MethodParameters parametersData)
            => new Type[] { typeof(ITypeWrapper<>).MakeGenericType(baseType) };

        FieldDefinition[] IImplementationProvider.GetFieldDefinitions(Type baseType, in MethodParameters constructorParameters)
            => new FieldDefinition[]
            { 
                new FieldDefinition("m_target", baseType)
                {
                    FieldAttributes = FieldAttributes.Private | FieldAttributes.InitOnly
                }
            };

        void IImplementationProvider.SetField(Type baseType, in FieldInitConfig fieldSetter)
            => fieldSetter.SetByArgument(1);

        void IImplementationProvider.WriteMethod(ReadOnlyCollection<FieldBuilder> fields, MethodInfo base_method, ReadOnlyCollection<Type> genericArgs, in MethodParameters methodParameters, MethodBuilder methodBuilder)
        {
            ILGenerator il = methodBuilder.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, fields[0]);

            if (!base_method.DeclaringType.IsConstructedGenericType || base_method.DeclaringType.GetGenericTypeDefinition() != typeof(ITypeWrapper<>))
            {
                for (int i = 0; i < methodParameters.m_parameterTypes.Length; i++)
                    il.Emit(OpCodes.Ldarg, i + 1);

                il.Emit(OpCodes.Callvirt, base_method);
            }

            il.Emit(OpCodes.Ret);
        }

        public static Func<T, T> GetWrapperFunction<T>()
            => ImplementationFactory.CreateFactory<Func<T, T>>(s_instance);

        public static bool TryUnwrap<T>(ref T target)
        {
            if (target is ITypeWrapper<T> wrapper)
            {
                target = wrapper.Target;
                return true;
            }
            return false;
        }
    }
}

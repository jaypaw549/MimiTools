using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace MimiTools.Implementer
{
    public class WrapperFactory : IImplProvider
    {
        private static readonly WrapperFactory s_instance = new WrapperFactory();

        private WrapperFactory() { }

        public Type[] GetAdditionalInterfaces(Type baseType, in ParametersData parametersData)
            => new Type[] { typeof(IWrapper<>).MakeGenericType(baseType) };

        public FieldDefinition[] GetFieldDefinitions(Type baseType, in ParametersData constructorParameters)
            => new FieldDefinition[]
            { 
                new FieldDefinition("m_target", baseType)
                {
                    FieldAttributes = FieldAttributes.Private | FieldAttributes.InitOnly
                }
            };

        public void SetField(Type baseType, in FieldSetter fieldSetter)
            => fieldSetter.SetByArgument(1);

        public void WriteMethod(ReadOnlyCollection<FieldBuilder> fields, MethodInfo base_method, ReadOnlyCollection<Type> genericArgs, in ParametersData methodParameters, MethodBuilder methodBuilder)
        {
            ILGenerator il = methodBuilder.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, fields[0]);

            if (!base_method.DeclaringType.IsConstructedGenericType || base_method.DeclaringType.GetGenericTypeDefinition() != typeof(IWrapper<>))
            {
                for (int i = 0; i < methodParameters.m_parameterTypes.Length; i++)
                    il.Emit(OpCodes.Ldarg, i + 1);

                il.Emit(OpCodes.Callvirt, base_method);
            }

            il.Emit(OpCodes.Ret);
        }

        public static Func<T, T> GetWrapperFunction<T>()
            => ImplFactory.CreateFactory<Func<T, T>>(typeof(T), s_instance);

        public static bool TryUnwrap<T>(ref T target)
        {
            if (target is IWrapper<T> wrapper)
            {
                target = wrapper.Target;
                return true;
            }
            return false;
        }
    }
}

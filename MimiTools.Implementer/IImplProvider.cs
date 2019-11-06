using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Reflection.Emit;

namespace MimiTools.Implementer
{
    public interface IImplProvider
    {
        Type[] GetAdditionalInterfaces(Type baseType, in ParametersData parametersData);

        FieldDefinition[] GetFieldDefinitions(Type baseType, in ParametersData constructorParameters);

        void SetField(Type baseType, in FieldSetter fieldSetter);

        void WriteMethod(ReadOnlyCollection<FieldBuilder> fields, MethodInfo base_method, ReadOnlyCollection<Type> genericArgs, in ParametersData methodParameters, MethodBuilder methodBuilder);
    }
}
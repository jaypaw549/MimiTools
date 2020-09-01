using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Reflection.Emit;

namespace MimiTools.Implementer
{
    public interface IImplementationProvider
    {
        Type[] GetAdditionalInterfaces(Type baseType, in MethodParameters parametersData);

        FieldDefinition[] GetFieldDefinitions(Type baseType, in MethodParameters constructorParameters);

        void SetField(Type baseType, in FieldInitConfig fieldInitConfig);

        void WriteMethod(ReadOnlyCollection<FieldBuilder> fields, MethodInfo base_method, ReadOnlyCollection<Type> genericArgs, in MethodParameters methodParameters, MethodBuilder methodBuilder);
    }
}
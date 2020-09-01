using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace MimiTools.Implementer
{
    internal struct ImplToolkit
    {
        internal AssemblyBuilder Assembly;

        internal Type BaseType;

        internal FieldBuilder[] Fields;

        internal Type FactoryDelegateType;

        internal MethodBuilder[] Methods;

        internal ModuleBuilder Module;

        internal MethodParameters ParametersData;

        internal IImplementationProvider Provider;

        internal TypeBuilder Type;
    }
}

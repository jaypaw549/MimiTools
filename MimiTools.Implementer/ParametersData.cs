using System;
using System.Collections.Generic;
using System.Text;

namespace MimiTools.Implementer
{
    public readonly struct ParametersData
    {


        public readonly IReadOnlyCollection<Type> ParameterTypes => m_parameterTypes;
        public readonly IReadOnlyCollection<IReadOnlyCollection<Type>> RequiredModifiers => m_requiredModifiers;
        public readonly IReadOnlyCollection<IReadOnlyCollection<Type>> OptionalModifiers => m_optionalModifiers;

        internal readonly Type[] m_parameterTypes;
        internal readonly Type[][] m_requiredModifiers;
        internal readonly Type[][] m_optionalModifiers;

        public ParametersData(Type[] parameterTypes, Type[][] requiredModifiers, Type[][] optionalModifiers)
        {
            m_parameterTypes = parameterTypes ?? throw new ArgumentNullException(nameof(parameterTypes));
            m_requiredModifiers = requiredModifiers ?? throw new ArgumentNullException(nameof(requiredModifiers));
            m_optionalModifiers = optionalModifiers ?? throw new ArgumentNullException(nameof(optionalModifiers));
        }
    }
}

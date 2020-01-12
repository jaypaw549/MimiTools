using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace MimiTools.Implementer
{
    public struct FieldDefinition
    {
        public FieldDefinition(string name, Type type)
        {
            Name = name;
            Type = type;

            RequiredCustomModifiers = Type.EmptyTypes;
            OptionalCustomModifiers = Type.EmptyTypes;
            
            m_attributes = FieldAttributes.Private;
        }

        private FieldAttributes m_attributes;

        public readonly string Name { get; }

        public readonly Type Type { get; }

        public Type[] RequiredCustomModifiers { readonly get; set; }

        public Type[] OptionalCustomModifiers { readonly get; set; }

        public FieldAttributes FieldAttributes
        {
            readonly get => m_attributes;
            set
            {
                if ((value & FieldAttributes.Static) == FieldAttributes.Static)
                    throw new InvalidOperationException("Static fields are not allowed!");

                m_attributes = value;
            }
        }
    }
}

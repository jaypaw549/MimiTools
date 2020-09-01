using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace MimiTools.Implementer
{
    public class ImplementationBuilder<TDelegate> where TDelegate : Delegate
    {
        public delegate void MethodWriter(ReadOnlyCollection<FieldBuilder> fields, MethodInfo base_method, ReadOnlyCollection<Type> genericArgs, in MethodParameters methodParameters, MethodBuilder methodBuilder);

        private delegate void FieldInitializationConfigurator(in FieldInitConfig config);

        private readonly List<FieldDefinition> m_fields = new List<FieldDefinition>();

        private readonly Dictionary<string, FieldInitializationConfigurator> m_field_configurators = new Dictionary<string, FieldInitializationConfigurator>();

        private readonly List<Type> m_interfaces = new List<Type>();

        private readonly ImplProvider m_provider;

        private readonly Dictionary<MethodInfo, MethodWriter> m_writerOverrides = new Dictionary<MethodInfo, MethodWriter>();

        private MethodWriter m_defaultWriter = NotImplemented;

        public ImplementationBuilder()
        { m_provider = new ImplProvider(this); }

        public ImplementationBuilder<TDelegate> AddField(in FieldDefinition field_def)
        {
            m_field_configurators.Add(field_def.Name, NotSet);
            m_fields.Add(field_def);
            return this;
        }

        public ImplementationBuilder<TDelegate> AddInterface(Type i)
        {
            m_interfaces.Add(i);
            return this;
        }

        public TDelegate Build()
            => ImplementationFactory.CreateFactory<TDelegate>(m_provider);

        public ImplementationBuilder<TDelegate> InitFieldFromArgument(string field, int argumentIndex)
        {
            if (m_field_configurators.ContainsKey(field))
                m_field_configurators[field] = delegate (in FieldInitConfig fieldConfig) { fieldConfig.SetByArgument(argumentIndex); };
            else
                throw new ArgumentException("Specified field does not exist!");

            return this;
        }

        public ImplementationBuilder<TDelegate> InitFieldFromConstant(string field, object constant)
        {
            if (m_field_configurators.ContainsKey(field))
                m_field_configurators[field] = delegate (in FieldInitConfig fieldConfig) { fieldConfig.SetConstantValue(constant); };
            else
                throw new ArgumentException("Specified field does not exist!");

            return this;
        }

        public ImplementationBuilder<TDelegate> InitFieldFromCustomMethod(string field, Action<MethodBuilder> methodGenerator)
        {
            if (m_field_configurators.ContainsKey(field))
                m_field_configurators[field] = delegate (in FieldInitConfig fieldConfig) { methodGenerator(fieldConfig.SetByCustomMethod()); };
            else
                throw new ArgumentException("Specified field does not exist!");

            return this;
        }

        public ImplementationBuilder<TDelegate> OverrideMethodWriter(MethodInfo target, MethodWriter writer)
        {
            m_writerOverrides[target] = writer;
            return this;
        }

        public ImplementationBuilder<TDelegate> SetMethodWriter(MethodWriter writer)
        {
            m_defaultWriter = writer;
            return this;
        }

        private static void NotImplemented(ReadOnlyCollection<FieldBuilder> fields, MethodInfo base_method, ReadOnlyCollection<Type> genericArgs, in MethodParameters methodParameters, MethodBuilder methodBuilder)
        {
            ILGenerator il = methodBuilder.GetILGenerator();
            il.Emit(OpCodes.Newobj, typeof(NotImplementedException).GetConstructor(Type.EmptyTypes));
            il.Emit(OpCodes.Throw);
        }

        private static void NotSet(in FieldInitConfig config)
            => config.DoNotSet();

        private class ImplProvider : IImplementationProvider
        {
            private readonly ImplementationBuilder<TDelegate> m_builder;

            internal ImplProvider(ImplementationBuilder<TDelegate> builder)
                => m_builder = builder;

            public Type[] GetAdditionalInterfaces(Type baseType, in MethodParameters parametersData)
                => m_builder.m_interfaces.ToArray();

            public FieldDefinition[] GetFieldDefinitions(Type baseType, in MethodParameters constructorParameters)
                => m_builder.m_fields.ToArray();

            public void SetField(Type baseType, in FieldInitConfig fieldInitConfig)
                => m_builder.m_field_configurators[fieldInitConfig.Field.Name](in fieldInitConfig);

            public void WriteMethod(ReadOnlyCollection<FieldBuilder> fields, MethodInfo base_method, ReadOnlyCollection<Type> genericArgs, in MethodParameters methodParameters, MethodBuilder methodBuilder)
            {
                if (m_builder.m_writerOverrides.TryGetValue(base_method, out MethodWriter writer))
                    writer(fields, base_method, genericArgs, in methodParameters, methodBuilder);
                else
                    m_builder.m_defaultWriter(fields, base_method, genericArgs, in methodParameters, methodBuilder);
            }
        }
    }
}

using MimiTools.Memory;
using System;
using System.Reflection;
using System.Reflection.Emit;

namespace MimiTools.Implementer
{
    public readonly ref struct FieldInitConfig
    {
        private readonly UnsafeReference<FieldDefinition> m_def;
        private readonly FieldBuilder m_field;
        private readonly ILGenerator m_il;
        private readonly UnsafeReference<ImplToolkit> m_toolkit;
        private readonly UnsafeReference<bool> m_used;

        public ref readonly FieldDefinition Field => ref m_def.Reference;

        public ref readonly MethodParameters Parameters => ref m_toolkit.Reference.ParametersData;

        internal FieldInitConfig(ref FieldDefinition def, FieldBuilder field, ILGenerator il, ref ImplToolkit toolkit, out bool used)
        {
            used = false;
            m_def = UnsafeReference.FromReference(def);
            m_field = field;
            m_il = il;
            m_toolkit = UnsafeReference.FromReference(toolkit);
            m_used = UnsafeReference.FromReference(used);
        }

        private void CheckoutOperation()
        {
            if (m_used.Value)
                throw new InvalidOperationException("Field has already had its setter defined!");

            m_used.Value = true;
        }

        public void DoNotSet()
            => CheckoutOperation();

        public void SetByArgument(int arg)
        {
            CheckoutOperation();

            m_il.Emit(OpCodes.Ldarg_0);
            m_il.Emit(OpCodes.Ldarg, arg);
            m_il.Emit(OpCodes.Stfld, m_field);
        }

        public MethodBuilder SetByCustomMethod()
        {
            CheckoutOperation();

            ref MethodParameters data = ref m_toolkit.Reference.ParametersData;
            MethodBuilder field_setter = m_toolkit.Reference.Type.DefineMethod(
                $"Init_{Field.Name}", 
                MethodAttributes.Private | MethodAttributes.Static, 
                CallingConventions.Standard, 
                Field.Type,
                Field.RequiredCustomModifiers,
                Field.OptionalCustomModifiers,
                data.m_parameterTypes,
                data.m_requiredModifiers,
                data.m_optionalModifiers);

            m_il.Emit(OpCodes.Ldarg_0);

            for (int i = 0; i < data.m_parameterTypes.Length; i++)
                m_il.Emit(OpCodes.Ldarg, i+1);

            m_il.Emit(OpCodes.Call, field_setter);
            m_il.Emit(OpCodes.Stfld, Field.Type);

            return field_setter;
        }

        public void SetConstantValue(object value)
        {
            CheckoutOperation();
            m_field.SetConstant(value);
        }
    }
}
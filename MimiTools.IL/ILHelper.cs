using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

namespace MimiTools.IL
{
    internal static class ILHelper
    {
        private static readonly Dictionary<short, OpCode> OpCodes = typeof(OpCodes).GetFields().Where(f => f.FieldType == typeof(OpCode))
            .Select(f => (OpCode)f.GetValue(null)).ToDictionary(op => op.Value);

        internal static int GetOperandSize(OperandType type)
        {
            switch (type)
            {
                case OperandType.InlineNone:
                    return 0;

                case OperandType.ShortInlineBrTarget:
                case OperandType.ShortInlineI:
                case OperandType.ShortInlineVar:
                    return 1;

                case OperandType.InlineVar:
                    return 2;

                case OperandType.InlineBrTarget:
                case OperandType.InlineField:
                case OperandType.InlineI:
                case OperandType.InlineMethod:
                case OperandType.InlineSig:
                case OperandType.InlineString:
                case OperandType.InlineSwitch:
                case OperandType.InlineTok:
                case OperandType.InlineType:
                case OperandType.ShortInlineR:
                    return 4;

                case OperandType.InlineI8:
                case OperandType.InlineR:
                    return 8;

                default:
                    return -1;
            }
        }

        internal static unsafe T ReadILValue<T>(void* target) where T : unmanaged
        {
            T value = default;
            byte* ptr = (byte*)&value;
            byte* tgt = (byte*)target;
            if (BitConverter.IsLittleEndian)
                value = *(T*)target;
            else
            {
                int max = sizeof(T) - 1;
                for (int i = 0; i <= max; i++)
                    ptr[i] = tgt[max - i];
            }

            return value;
        }

        internal static unsafe OpCode ReadOpCode(void* target)
        {
            byte* ptr = (byte*)target;
            short value = *ptr;

            if (!OpCodes.TryGetValue(value, out OpCode op))
                throw new ArgumentOutOfRangeException("Specified Opcode is invalid!");

            if (op.OpCodeType == OpCodeType.Nternal)
            {
                value <<= 8;
                value |= (short)ptr[1];
                if (!OpCodes.TryGetValue(value, out op))
                    throw new ArgumentOutOfRangeException("Specified Opcode is invalid!");
            }

            return op;
        }

        internal static unsafe int WriteILValue<T>(void* target, T value) where T : unmanaged
        {
            byte* ptr = (byte*)&value;
            byte* tgt = (byte*)target;
            if (BitConverter.IsLittleEndian)
                *(T*)target = value;
            else
            {
                int max = sizeof(T) - 1;
                for (int i = 0; i <= max; i++)
                    tgt[i] = ptr[max - i];
            }

            return sizeof(T);
        }

        internal static unsafe int WriteOpCode(void* target, OpCode op)
        {
            byte* ptr = (byte*)target;
            short value = op.Value;

            for (int i = op.Size - 1; i >= 0; i--)
            {
                *ptr = (byte)(value >> (8 * i));
                ptr++;
            }

            return op.Size;
        }
    }
}

using MimiTools.Collections.Weak;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace MimiTools.IL
{
    internal static class ILHelper
    {
        private static readonly Dictionary<short, OpCode> OpCodes = typeof(OpCodes).GetFields().Where(f => f.FieldType == typeof(OpCode))
            .Select(f => (OpCode)f.GetValue(null)).ToDictionary(op => op.Value);

        private static readonly WeakDictionary<MethodBase, byte[]> data = new WeakDictionary<MethodBase, byte[]>();

        internal static byte[] GetBody(MethodBase source)
        {
            lock (data)
            {
                if (data.TryGetValue(source, out byte[] il))
                    return il;
                return data[source] = source.GetMethodBody().GetILAsByteArray();
            }
        }

        internal static OpCode GetEmitOpCode(byte[] data, int offset)
        {
            short value = data[offset];
            if (!OpCodes.TryGetValue(value, out OpCode op))
                throw new BadImageFormatException();

            if (op.OpCodeType == OpCodeType.Nternal)
            {
                value = (short)(((uint)value << (sizeof(byte) * 8)) | data[offset + 1]);

                if (!OpCodes.TryGetValue(value, out op))
                    throw new BadImageFormatException();
            }

            return op;
        }

        internal static int GetOpLength(byte[] data, int offset)
        {
            OpCode op = GetEmitOpCode(data, offset);
            return op.Size + GetOperandLength(op, data, offset);
        }

        internal static int GetOpLength(in OpCode op, object data)
            => op.OperandType switch
            {
                OperandType.InlineSwitch => op.Size + sizeof(int) + (((Array)data).Length * sizeof(int)),
                _ => op.Size + GetOperandLength(op, null, 0)
            };

        internal static int GetOperandLength(byte[] data, int offset)
            => GetOperandLength(GetEmitOpCode(data, offset), data, offset);

        private static int GetOperandLength(in OpCode op, byte[] data, int offset)
        {
            switch (op.OperandType)
            {
                case OperandType.InlineNone:
                    return 0;
                case OperandType.ShortInlineBrTarget:
                case OperandType.ShortInlineI:
                case OperandType.ShortInlineVar:
                    return 1;
                case OperandType.InlineBrTarget:
                case OperandType.InlineVar:
                    return 2;
                case OperandType.InlineField:
                case OperandType.InlineI:
                case OperandType.InlineMethod:
                case OperandType.InlineSig:
                case OperandType.InlineString:
                case OperandType.InlineTok:
                case OperandType.InlineType:
                case OperandType.ShortInlineR:
                    return 4;
                case OperandType.InlineI8:
                case OperandType.InlineR:
                    return 8;
                case OperandType.InlineSwitch:
                    return BitConverter.ToInt32(data, offset + op.Size) + sizeof(int);
                default:
                    throw new InvalidOperationException("An Unknown Error occurred!");
            }
        }

        internal static object GetOperandValue(MethodBase body, int offset, bool allow_local_resolve)
        {
            byte[] data = GetBody(body);
            OpCode code = GetEmitOpCode(data, offset);
            int operand = offset + code.Size;
            switch(code.OperandType)
            {
                case OperandType.InlineBrTarget:
                    if (allow_local_resolve)
                        return new Op(body, operand + GetOperandLength(data, offset) + BitConverter.ToInt32(data, operand));
                    return BitConverter.ToInt32(data, operand);
                case OperandType.InlineField:
                    return body.Module.ResolveField(BitConverter.ToInt32(data, operand), body.DeclaringType.GetGenericArguments(), body.GetGenericArguments());
                case OperandType.InlineI:
                    return BitConverter.ToInt32(data, operand);
                case OperandType.InlineI8:
                    return BitConverter.ToInt64(data, operand);
                case OperandType.InlineMethod:
                    return body.Module.ResolveMethod(BitConverter.ToInt32(data, operand), body.DeclaringType.GetGenericArguments(), body.GetGenericArguments());
                case OperandType.InlineNone:
                    return null;
#pragma warning disable CS0618 // Type or member is obsolete
                case OperandType.InlinePhi:
                    throw new NotSupportedException();
#pragma warning restore CS0618 // Type or member is obsolete
                case OperandType.InlineR:
                    return BitConverter.ToDouble(data, operand);
                case OperandType.InlineSig:
                    return body.Module.ResolveSignature(BitConverter.ToInt32(data, operand));
                case OperandType.InlineString:
                    return body.Module.ResolveString(BitConverter.ToInt32(data, operand));
                case OperandType.InlineSwitch:
                    if (allow_local_resolve)
                        return ResolveSwitchTargets(body, data, offset, operand);
                    return GetSwitchTargetOffsets(body, data, offset, operand);
                case OperandType.InlineTok:
                    return body.Module.ResolveMember(BitConverter.ToInt32(data, operand), body.DeclaringType.GetGenericArguments(), body.GetGenericArguments());
                case OperandType.InlineType:
                    return body.Module.ResolveType(BitConverter.ToInt32(data, operand), body.DeclaringType.GetGenericArguments(), body.GetGenericArguments());
                case OperandType.InlineVar:
                    if (allow_local_resolve)
                        return body.GetMethodBody().LocalVariables[BitConverter.ToInt32(data, operand)];
                    return BitConverter.ToInt32(data, operand);
                case OperandType.ShortInlineBrTarget:
                    if (allow_local_resolve)
                        return new Op(body, operand + GetOperandLength(data, offset) + (sbyte) data[operand]);
                    return (sbyte)data[operand];
                case OperandType.ShortInlineI:
                    return data[operand];
                case OperandType.ShortInlineR:
                    return BitConverter.ToSingle(data, operand);
                case OperandType.ShortInlineVar:
                    if (allow_local_resolve)
                        return body.GetMethodBody().LocalVariables[data[operand]];
                    return data[operand];
                default:
                    throw new InvalidOperationException("Unknown operand type!");
            }
        }

        private static int[] GetSwitchTargetOffsets(MethodBase body, byte[] data, int offset, int operand)
        {
            int[] targets = new int[BitConverter.ToInt32(data, operand)];
            int start = GetOperandLength(data, offset) + operand;
            for (int i = 0; i < targets.Length; i++)
            {
                operand += sizeof(int);
                targets[i] = start + BitConverter.ToInt32(data, operand);
            }
            return targets;
        }

        private static Op[] ResolveSwitchTargets(MethodBase body, byte[] data, int offset, int operand)
        {
            Op[] targets = new Op[BitConverter.ToInt32(data, operand)];
            int start = GetOperandLength(data, offset) + operand;
            for (int i = 0; i < targets.Length; i++)
            {
                operand += sizeof(int);
                targets[i] = new Op(body, start + BitConverter.ToInt32(data, operand));
            }
            return targets;
        }
    }
}

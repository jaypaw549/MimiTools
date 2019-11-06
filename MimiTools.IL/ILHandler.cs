using System.Collections.Generic;
using System.Reflection.Emit;

namespace MimiTools.IL
{
    public static class ILHandler
    {
        public static unsafe Op[] ReadIL(byte[] raw_il)
        {
            fixed (byte* ptr = raw_il)
                return ReadIL(ptr, raw_il.LongLength);
        }

        public static unsafe Op[] ReadIL(byte* il_ptr, long count)
        {
            List<Op> operations = new List<Op>();
            void* end = il_ptr + count;
            while (il_ptr < end)
            {
                OpCode code = ILHelper.ReadOpCode(il_ptr);

                il_ptr += code.Size;
                byte[] operand = ReadOperand(il_ptr, code.OperandType);
                il_ptr += operand.Length;

                operations.Add(new Op(code, operand));
            }

            return operations.ToArray();
        }

        public static unsafe Op ReadOp(byte[] raw_il, int offset)
        {
            fixed (byte* b = raw_il)
                return ReadOp(b + offset);
        }

        public static unsafe Op ReadOp(byte* il_ptr)
        {
            OpCode op = ILHelper.ReadOpCode(il_ptr);
            il_ptr += op.Size;
            return new Op(op, ReadOperand(il_ptr, op.OperandType));
        }

        private static unsafe byte[] ReadOperand(byte* ptr, OperandType type)
        {
            int size = ILHelper.GetOperandSize(type);
            byte[] data;

            fixed (byte* b = data = new byte[size])
                for (int i = 0; i < size; i++)
                    b[i] = ptr[i];

            return data;
        }
    }
}

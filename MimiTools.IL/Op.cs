using System.Linq;
using System.Reflection.Emit;

namespace MimiTools.IL
{
    public unsafe struct Op
    {
        private readonly OpCode _op;
        private fixed byte _operand[8];

        internal Op(OpCode op, byte[] operand)
        {
            _op = op;
            fixed (byte* b = operand)
                for (int i = 0; i < operand.Length; i++)
                    _operand[i] = b[i];
        }

        public string Name => _op.Name;
        public short OpCode => _op.Value;
        public int OpCodeSize => _op.Size;

        public byte[] Operand
        {
            get
            {
                byte[] data = new byte[OperandSize];
                WriteOperand(data, 0);
                return data;
            }
        }

        public int OperandSize => ILHelper.GetOperandSize(_op.OperandType);
        public int Size => _op.Size + ILHelper.GetOperandSize(_op.OperandType);

        public byte[] AsByteArray()
        {
            byte[] data = new byte[Size];
            WriteBytes(data, 0);
            return data;
        }

        public T ReadOperand<T>() where T : unmanaged
        {
            fixed (byte* ptr = _operand)
                return ILHelper.ReadILValue<T>(ptr);
        }

        public override string ToString()
            => $"{Name}{string.Join(string.Empty, Operand.Select(b => " " + b.ToString("X2")))}";

        public int WriteBytes(byte[] array, int index)
        {
            int before = index;
            index += WriteOpCode(array, index);
            index += WriteOperand(array, index);
            return index - before;
        }

        public int WriteOpCode(byte[] array, int index)
        {
            int before = index;
            short val = _op.Value;

            fixed (byte* a = array)
                for (int i = _op.Size - 1; i >= 0; i--)
                    a[index++] = (byte)(val >> (i * 8));

            return index - before;
        }

        public int WriteOperand(byte[] array, int index)
        {
            int before = index;
            short val = _op.Value;

            fixed (byte* a = array)
            {
                int size = ILHelper.GetOperandSize(_op.OperandType);
                for (int i = 0; i < size; i++)
                    a[index++] = _operand[i];
            }

            return index - before;
        }
    }
}

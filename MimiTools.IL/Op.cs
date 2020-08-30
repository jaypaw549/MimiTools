using MimiTools.Collections.Weak;
using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace MimiTools.IL
{
    public readonly struct Op
    {
        internal Op(MethodBase method, int offset)
        {
            source = method;
            this.offset = offset;
        }

        private readonly MethodBase source;
        private readonly int offset;

        public OpCode EmitOpCode => ILHelper.GetEmitOpCode(ILHelper.GetBody(source), offset);

        public MethodBase Method => source;

        public int Offset => offset;

        public object Operand => ILHelper.GetOperandValue(source, offset, true);

        public int Size => ILHelper.GetOpLength(ILHelper.GetBody(source), offset);

        public OpData ToData()
            => new OpData(EmitOpCode, ILHelper.GetOperandValue(source, offset, false));

        public override string ToString()
        {
            string operand = Operand?.ToString();
            if (operand != null)
                return $"IL_{offset.ToString("X4")}: {EmitOpCode.Name} {operand}";
            return $"IL_{offset.ToString("X4")}: {EmitOpCode.Name}";
        }
    }

    public readonly struct OpData
    {
        internal OpData(OpCode op, object operand)
        {
            this.op = op;
            this.operand = operand;
        }

        private readonly OpCode op;
        private readonly object operand;

        public OpCode EmitOpCode => op;
        public object Operand => operand;

        public int Size => ILHelper.GetOpLength(op, operand);

        public override string ToString()
        {
            string operand = this.operand?.ToString();
            if (operand != null)
                return $"{EmitOpCode.Name} {operand}";
            return op.Name;
        }
    }
}

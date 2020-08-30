using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace MimiTools.IL
{
    public static class ILHandler
    {
        public static int GetInstructionCount(this MethodBase method)
        {
            byte[] data = ILHelper.GetBody(method);

            int i = 0;
            int ptr = 0;
            while (ptr < data.Length)
            {
                i++;
                ptr += ILHelper.GetOpLength(data, ptr);
            }

            return i;
        }

        public static Op[] GetMethodIL(this MethodBase method)
        {
            byte[] data = ILHelper.GetBody(method);
            Op[] ops = new Op[method.GetInstructionCount()];

            int i = 0;
            int ptr = 0;
            while (ptr < data.Length)
            {
                ops[i++] = new Op(method, ptr);
                ptr += ILHelper.GetOpLength(data, ptr);
            }

            return ops;
        }
    }
}

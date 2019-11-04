using MimiTools.Expressions.Components;

namespace MimiTools.Expressions
{
    public abstract class ExpressionReader
    {
        public abstract bool TryReadExpression(CodeReader reader, CodeSection code, out IExpression e);
    }
}

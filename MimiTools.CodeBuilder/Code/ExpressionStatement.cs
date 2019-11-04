using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;

namespace MimiTools.CodeBuilder.Code
{
    internal sealed class ExpressionStatement : Statement
    {
        private readonly Expression expression;

        internal ExpressionStatement(Expression expression)
        {
            this.expression = expression;
        }

        private protected override SyntaxNode Build(SyntaxGenerator generator)
            => generator.ExpressionStatement(expression.BuildInternal(generator));
    }
}

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;

namespace MimiTools.CodeBuilder.Code
{
    internal sealed class ThrowStatement : Statement
    {
        private readonly Expression exception;

        internal ThrowStatement(Expression exception)
        {
            this.exception = exception;
        }

        private protected override SyntaxNode Build(SyntaxGenerator generator)
            => generator.ThrowStatement(exception.BuildInternal(generator));
    }
}

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;

namespace MimiTools.CodeBuilder.Code
{
    internal sealed class TryFinallyStatement : Statement
    {
        private readonly Statement[] statements;
        private readonly TryStatement original;

        internal TryFinallyStatement(TryStatement original, Statement[] statements)
        {
            this.original = original;
            this.statements = statements;
        }

        private protected override SyntaxNode Build(SyntaxGenerator generator)
            => original.BuildWithFinally(generator, statements);
    }
}

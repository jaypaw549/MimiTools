using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;

namespace MimiTools.CodeBuilder.Code
{
    internal sealed class WhileStatement : Statement
    {
        private readonly Expression condition;
        private readonly Statement[] body;

        internal WhileStatement(Expression condition, Statement[] body)
        {
            this.condition = condition;
            this.body = body;
        }

        private protected override SyntaxNode Build(SyntaxGenerator generator)
            => generator.WhileStatement(condition.BuildInternal(generator), BuildMany(generator, body));
    }
}
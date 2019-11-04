using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;

namespace MimiTools.CodeBuilder.Code
{
    internal sealed class AssignmentStatement : Statement
    {
        private readonly Expression target;
        private readonly Expression value;

        internal AssignmentStatement(Expression target, Expression value)
        {
            this.target = target;
            this.value = value;
        }

        private protected override SyntaxNode Build(SyntaxGenerator generator)
            => generator.AssignmentStatement(target.BuildInternal(generator), value.BuildInternal(generator));
    }
}

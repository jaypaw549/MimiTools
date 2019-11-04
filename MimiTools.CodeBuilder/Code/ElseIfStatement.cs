using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;
using System.Collections.Generic;

namespace MimiTools.CodeBuilder.Code
{
    internal sealed class ElseIfStatement : IfStatement
    {
        private readonly IfStatement original;

        internal ElseIfStatement(IfStatement original, Expression condition, Statement[] if_true) : base(condition, if_true)
        {
            this.original = original;
        }

        private protected override SyntaxNode Build(SyntaxGenerator generator)
            => original.WrapBuild(generator, base.Build(generator));

        internal override SyntaxNode BuildWithElse(SyntaxGenerator generator, IEnumerable<Statement> on_false)
            => original.WrapBuild(generator, base.BuildWithElse(generator, on_false));

        internal override SyntaxNode WrapBuild(SyntaxGenerator generator, SyntaxNode inner)
            => WrapBuild(generator, generator.IfStatement(condition.BuildInternal(generator), BuildTrue(generator), inner));
    }
}

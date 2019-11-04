using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;

namespace MimiTools.CodeBuilder.Code
{
    internal sealed class ElseStatement : Statement
    {
        private readonly IfStatement original;
        private readonly Statement if_false;
        internal ElseStatement(IfStatement original, Statement if_false)
        {
            this.original = original;
            this.if_false = if_false;
        }

        private protected override SyntaxNode Build(SyntaxGenerator generator)
            => original.WrapBuild(generator, if_false.BuildInternal(generator));
    }
}

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;
using System.Collections.Generic;

namespace MimiTools.CodeBuilder.Code
{
    public class IfStatement : Statement
    {
        private protected readonly Expression condition;
        private protected readonly Statement[] if_true;

        internal IfStatement(Expression condition, Statement[] if_true)
        {
            this.condition = condition;
            this.if_true = if_true;
        }

        private protected override SyntaxNode Build(SyntaxGenerator generator)
            => generator.IfStatement(condition.BuildInternal(generator), BuildTrue(generator));

        private protected IEnumerable<SyntaxNode> BuildTrue(SyntaxGenerator generator)
            => BuildMany(generator, if_true);

        internal virtual SyntaxNode BuildWithElse(SyntaxGenerator generator, IEnumerable<Statement> on_false)
            => generator.IfStatement(condition.BuildInternal(generator), BuildTrue(generator), BuildMany(generator, on_false));

        public Statement Else(Statement if_false)
            => new ElseStatement(this, if_false);

        public Statement Else(params Statement[] if_false)
            => new ElseBlockStatement(this, if_false);

        public IfStatement ElseIf(Expression condition, params Statement[] if_true)
           => new ElseIfStatement(this, condition, if_true);

        internal virtual SyntaxNode WrapBuild(SyntaxGenerator generator, SyntaxNode on_false)
            => generator.IfStatement(condition.BuildInternal(generator), BuildTrue(generator), on_false);
    }
}

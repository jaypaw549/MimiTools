using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;

namespace MimiTools.CodeBuilder.Code
{
    internal sealed class LockStatement : Statement
    {
        private readonly Expression lock_obj;
        private readonly Statement[] statements;

        internal LockStatement(Expression lock_obj, Statement[] statements)
        {
            this.lock_obj = lock_obj;
            this.statements = statements;
        }

        private protected override SyntaxNode Build(SyntaxGenerator generator)
            => generator.LockStatement(lock_obj.BuildInternal(generator), BuildMany(generator, statements));
    }
}

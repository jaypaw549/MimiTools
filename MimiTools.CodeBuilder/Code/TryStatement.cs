using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;
using System.Collections.Generic;

namespace MimiTools.CodeBuilder.Code
{
    public class TryStatement : Statement
    {
        private protected readonly Statement[] statements;

        internal TryStatement(Statement[] statements)
        {
            this.statements = statements;
        }

        private protected override SyntaxNode Build(SyntaxGenerator generator)
            => throw new System.InvalidOperationException("There are no catch or finally clauses!");

        internal SyntaxNode BuildWithCatch(SyntaxGenerator generator, IEnumerable<SyntaxNode> catches, IEnumerable<Statement> finally_clause)
            => generator.TryCatchStatement(BuildMany(generator, statements), catches, BuildMany(generator, finally_clause));

        internal virtual SyntaxNode BuildWithFinally(SyntaxGenerator generator, IEnumerable<Statement> finally_clause)
            => generator.TryFinallyStatement(BuildMany(generator, statements), BuildMany(generator, finally_clause));

        public TryStatement Catch(ReferenceBuilder type, string name, params Statement[] statements)
            => new TryCatchStatement(this, type, name, statements);

        public Statement Finally(params Statement[] statements)
            => new TryFinallyStatement(this, statements);
    }
}

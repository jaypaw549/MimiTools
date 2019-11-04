using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;
using System.Collections.Generic;

namespace MimiTools.CodeBuilder.Code
{
    internal sealed class TryCatchStatement : TryStatement
    {
        private readonly TryStatement original;
        private readonly ReferenceBuilder type;
        private readonly string name;

        internal TryCatchStatement(TryStatement original, ReferenceBuilder type, string name, Statement[] statements) : base(statements)
        {
            this.original = original;
            this.name = name;
            this.type = type;
        }

        private protected override SyntaxNode Build(SyntaxGenerator generator)
            => ChainBuild(generator, null);

        private SyntaxNode BuildClause(SyntaxGenerator generator)
            => generator.CatchClause(type.BuildInternal(generator), name, BuildMany(generator, statements));

        internal override SyntaxNode BuildWithFinally(SyntaxGenerator generator, IEnumerable<Statement> finally_clause)
            => ChainBuild(generator, finally_clause);

        private SyntaxNode ChainBuild(SyntaxGenerator generator, IEnumerable<Statement> finally_clause)
        {
            Stack<SyntaxNode> catches = new Stack<SyntaxNode>();
            TryStatement statement = this;
            while (statement is TryCatchStatement c)
            {
                catches.Push(c.BuildClause(generator));
                statement = c.original;
            }

            return statement.BuildWithCatch(generator, catches, finally_clause);
        }
    }
}

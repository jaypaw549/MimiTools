using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;

namespace MimiTools.CodeBuilder.Code
{
    internal sealed class NoVarUsingStatement : Statement
    {
        private readonly Expression obj;
        private readonly Statement[] body;

        internal NoVarUsingStatement(Expression obj, Statement[] body)
        {
            this.obj = obj;
            this.body = body;
        }

        private protected override SyntaxNode Build(SyntaxGenerator generator)
            => generator.UsingStatement(obj.BuildInternal(generator), BuildMany(generator, body));
    }
}

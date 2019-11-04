using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;

namespace MimiTools.CodeBuilder.Code
{
    internal sealed class VarUsingStatement : Statement
    {
        private readonly string name;
        private readonly Expression obj;
        private readonly Statement[] body;

        internal VarUsingStatement(string name, Expression obj, Statement[] body)
        {
            this.name = name;
            this.obj = obj;
            this.body = body;
        }

        private protected override SyntaxNode Build(SyntaxGenerator generator)
            => generator.UsingStatement(name, obj.BuildInternal(generator), BuildMany(generator, body));
    }
}

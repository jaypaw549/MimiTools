using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;

namespace MimiTools.CodeBuilder.Code
{
    internal sealed class FullUsingStatement : Statement
    {
        private readonly ReferenceBuilder type;
        private readonly string name;
        private readonly Expression obj;
        private readonly Statement[] body;

        public FullUsingStatement(ReferenceBuilder type, string name, Expression obj, Statement[] body)
        {
            this.type = type;
            this.name = name;
            this.obj = obj;
            this.body = body;
        }

        private protected override SyntaxNode Build(SyntaxGenerator generator)
            => generator.UsingStatement(type.BuildInternal(generator), name, obj.BuildInternal(generator), BuildMany(generator, body));
    }
}

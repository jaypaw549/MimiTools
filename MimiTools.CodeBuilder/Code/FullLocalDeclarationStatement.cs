using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;

namespace MimiTools.CodeBuilder.Code
{
    internal sealed class FullLocalDeclarationStatement : LocalDeclarationStatement
    {
        private readonly ReferenceBuilder type;
        private readonly bool is_const;

        internal FullLocalDeclarationStatement(string name, ReferenceBuilder type, Expression initializer, bool is_const) : base(name, initializer)
        {
            this.type = type;
            this.is_const = is_const;
        }

        private protected override SyntaxNode Build(SyntaxGenerator generator)
            => generator.LocalDeclarationStatement(type.BuildInternal(generator), name, initializer.BuildInternal(generator), is_const);
    }
}

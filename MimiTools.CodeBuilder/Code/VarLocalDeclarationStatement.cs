using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;

namespace MimiTools.CodeBuilder.Code
{
    internal sealed class VarLocalDeclarationStatement : LocalDeclarationStatement
    {
        internal VarLocalDeclarationStatement(string name, Expression initializer) : base(name, initializer)
        {
        }

        private protected override SyntaxNode Build(SyntaxGenerator generator)
            => generator.LocalDeclarationStatement(name, initializer.BuildInternal(generator));
    }
}

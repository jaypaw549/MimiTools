using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;

namespace MimiTools.CodeBuilder
{
    public sealed class StructBuilder : NestableTypeBuilder<StructBuilder>
    {
        private protected override SyntaxNode Build(SyntaxGenerator generator)
            => ConstrainTypeParameters(generator.StructDeclaration(Name,
                GetTypeParameterNames(),
                Accessibility,
                DeclarationModifiers,
                BuildInterfaces(generator),
                BuildMembers(generator)), generator);
    }
}
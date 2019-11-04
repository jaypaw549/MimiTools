using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;

namespace MimiTools.CodeBuilder
{
    public sealed class InterfaceBuilder : TypeBuilder<InterfaceBuilder>
    {
        private protected override SyntaxNode Build(SyntaxGenerator generator)
            => ConstrainTypeParameters(generator.InterfaceDeclaration(Name, GetTypeParameterNames(), Accessibility, BuildInterfaces(generator), BuildMembers(generator)), generator);
    }
}

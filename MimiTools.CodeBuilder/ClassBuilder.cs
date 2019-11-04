using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;

namespace MimiTools.CodeBuilder
{
    public sealed class ClassBuilder : NestableTypeBuilder<ClassBuilder>
    {
        public ReferenceBuilder BaseType { get; set; }

        private protected override SyntaxNode Build(SyntaxGenerator generator)
            => ConstrainTypeParameters(generator.ClassDeclaration(
                Name,
                GetTypeParameterNames(),
                Accessibility,
                DeclarationModifiers,
                BaseType.BuildInternal(generator),
                BuildInterfaces(generator),
                BuildMembers(generator)), generator);

        public ClassBuilder SetBaseType(ReferenceBuilder base_type_reference)
        {
            BaseType = base_type_reference;
            return this;
        }
    }
}
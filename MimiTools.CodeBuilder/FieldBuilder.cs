using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;
using MimiTools.CodeBuilder.Code;

namespace MimiTools.CodeBuilder
{
    public sealed class FieldBuilder : NodeBuilder
    {
        public Accessibility Accessibility { get; set; }

        public DeclarationModifiers DeclarationModifiers { get; set; }

        public Expression Initializer { get; set; }

        public string Name { get; set; }

        public ReferenceBuilder Type { get; set; }

        private protected override SyntaxNode Build(SyntaxGenerator generator)
            => generator.FieldDeclaration(Name, Type.BuildInternal(generator), Accessibility, DeclarationModifiers);

        public override void Clear()
        {
            Accessibility = default;
            DeclarationModifiers = default;
            Name = default;
            Type = default;
        }

        public FieldBuilder SetAccessibility(Accessibility accessibility)
        {
            Accessibility = accessibility;
            return this;
        }

        public FieldBuilder SetDeclarationModifiers(DeclarationModifiers modifiers)
        {
            DeclarationModifiers = modifiers;
            return this;
        }

        public FieldBuilder SetName(string name)
        {
            Name = name;
            return this;
        }

        public FieldBuilder SetType(ReferenceBuilder type)
        {
            Type = type;
            return this;
        }
    }
}
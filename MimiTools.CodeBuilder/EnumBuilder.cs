using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;

namespace MimiTools.CodeBuilder
{
    public sealed class EnumBuilder : NestableBuilder
    {
        internal EnumBuilder() { }

        public string Name { get; set; }
        public Accessibility Accessibility { get; set; }
        public DeclarationModifiers DeclarationModifiers { get; set; }

        public EnumBuilder AddMember(string name, int? value = null)
        {
            children.Add(new EnumMemberBuilder() { Name = name, Value = value });
            return this;
        }

        public EnumBuilder AddMember(EnumMemberBuilder builder)
        {
            children.Add(builder);
            return this;
        }

        private protected override SyntaxNode Build(SyntaxGenerator generator)
            => generator.EnumDeclaration(Name, Accessibility, DeclarationModifiers, BuildChildren(generator));

        public override void Clear()
        {
            base.Clear();
            Name = null;
            Accessibility = default;
            DeclarationModifiers = default;
        }

        public EnumBuilder SetAccessibility(Accessibility accessibility)
        {
            Accessibility = accessibility;
            return this;
        }

        public EnumBuilder SetDeclarationModifiers(DeclarationModifiers modifiers)
        {
            DeclarationModifiers = modifiers;
            return this;
        }

        public EnumBuilder SetName(string name)
        {
            Name = name;
            return this;
        }
    }
}
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;

namespace MimiTools.CodeBuilder
{
    public sealed class SpecialTypeReferenceBuilder : ReferenceBuilder
    {
        public SpecialType Type { get; set; }

        public override void Clear()
        {
            Type = default;
        }

        private protected override SyntaxNode Build(SyntaxGenerator generator)
            => generator.TypeExpression(Type);

        public SpecialTypeReferenceBuilder SetType(SpecialType type)
        {
            Type = type;
            return this;
        }
    }
}

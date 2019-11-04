using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;

namespace MimiTools.CodeBuilder
{
    public sealed class ArrayTypeReferenceBuilder : ReferenceBuilder
    {
        public ReferenceBuilder Type { get; set; }

        private protected override SyntaxNode Build(SyntaxGenerator generator)
            => generator.ArrayTypeExpression(Type.BuildInternal(generator));

        public override void Clear()
        {
            Type = null;
        }

        public ArrayTypeReferenceBuilder SetType(ReferenceBuilder reference)
        {
            Type = reference;
            return this;
        }
    }
}

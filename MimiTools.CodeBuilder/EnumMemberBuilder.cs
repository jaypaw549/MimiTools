using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;

namespace MimiTools.CodeBuilder
{
    public sealed class EnumMemberBuilder : NodeBuilder
    {

        public string Name { get; set; }

        public int? Value { get; set; }

        private protected override SyntaxNode Build(SyntaxGenerator generator)
            => generator.EnumMember(Name, Value.HasValue ? generator.LiteralExpression(Value) : null);

        public override void Clear()
        {
            Name = null;
            Value = null;
        }

        public EnumMemberBuilder SetName(string name)
        {
            Name = name;
            return this;
        }

        public EnumMemberBuilder SetValue(int? value)
        {
            Value = value;
            return this;
        }
    }
}

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;

namespace MimiTools.CodeBuilder
{
    public sealed class UnitBuilder : NestableBuilder
    {
        public UnitBuilder AddNode(NodeBuilder node)
        {
            children.Add(node);
            return this;
        }

        private protected override SyntaxNode Build(SyntaxGenerator generator)
            => generator.CompilationUnit(BuildChildren(generator));
    }
}

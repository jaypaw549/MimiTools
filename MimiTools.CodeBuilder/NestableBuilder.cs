using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;
using System.Collections.Generic;

namespace MimiTools.CodeBuilder
{
    public abstract class NestableBuilder : NodeBuilder
    {
        internal NestableBuilder() { }

        private protected readonly List<NodeBuilder> children = new List<NodeBuilder>();

        private protected IEnumerable<SyntaxNode> BuildChildren(SyntaxGenerator generator)
            => BuildMany(generator, children);

        public override void Clear()
            => children.Clear();
    }
}

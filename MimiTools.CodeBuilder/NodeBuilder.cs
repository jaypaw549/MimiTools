using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MimiTools.CodeBuilder
{
    public abstract class NodeBuilder
    {
        private protected NodeBuilder() { }
        private protected abstract SyntaxNode Build(SyntaxGenerator generator);

        internal SyntaxNode BuildInternal(SyntaxGenerator generator)
            => Build(generator);

        public abstract void Clear();

        public SyntaxNode CreateNodes(SyntaxGenerator generator)
            => Build(generator);

        public void WriteSource(TextWriter writer, string language)
        {
            using (AdhocWorkspace workspace = new AdhocWorkspace())
                Build(SyntaxGenerator.GetGenerator(workspace, language)).NormalizeWhitespace().WriteTo(writer);
        }

        private protected static IEnumerable<SyntaxNode> BuildMany(SyntaxGenerator generator, IEnumerable<NodeBuilder> nodes)
            => nodes?.Select(n => n.Build(generator));
    }
}

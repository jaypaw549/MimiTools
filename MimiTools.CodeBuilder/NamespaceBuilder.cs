using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;

namespace MimiTools.CodeBuilder
{
    public sealed class NamespaceBuilder : NestableBuilder
    {
        public string Name { get; set; }

        public NamespaceBuilder AddNamespace(NamespaceBuilder builder)
        {
            children.Add(builder);
            return this;
        }

        public NamespaceBuilder AddClass(ClassBuilder builder)
        {
            children.Add(builder);
            return this;
        }

        public NamespaceBuilder AddDelegate(DelegateBuilder builder)
        {
            children.Add(builder);
            return this;
        }

        public NamespaceBuilder AddEnum(EnumBuilder builder)
        {
            children.Add(builder);
            return this;
        }

        public NamespaceBuilder AddStruct(StructBuilder builder)
        {
            children.Add(builder);
            return this;
        }

        private protected override SyntaxNode Build(SyntaxGenerator generator)
            => generator.NamespaceDeclaration(Name, BuildChildren(generator));
    }
}

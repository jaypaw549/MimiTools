using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;
using System.Collections.Generic;
using System.Linq;

namespace MimiTools.CodeBuilder
{
    public sealed class DelegateBuilder : NodeBuilder
    {
        public Accessibility Accessibility { get; set; }

        public DeclarationModifiers DeclarationModifiers { get; set; }

        public string Name { get; set; }

        public List<ParameterBuilder> Parameters { get; set; } = new List<ParameterBuilder>();

        public ReferenceBuilder ReturnType { get; set; }

        public List<TypeParameterConfig> TypeParameters { get; set; } = new List<TypeParameterConfig>();

        public override void Clear()
        {
            Accessibility = default;
            DeclarationModifiers = default;
            Name = default;

            if (Parameters == null)
                Parameters = new List<ParameterBuilder>();
            else
                Parameters.Clear();

            ReturnType = default;

            if (TypeParameters == null)
                TypeParameters = new List<TypeParameterConfig>();
            else
                TypeParameters.Clear();
        }

        private protected override SyntaxNode Build(SyntaxGenerator generator)
            => generator.DelegateDeclaration(
                Name,
                BuildMany(generator, Parameters),
                TypeParameters.Select(p => p.Name),
                ReturnType.BuildInternal(generator),
                Accessibility,
                DeclarationModifiers);
    }
}
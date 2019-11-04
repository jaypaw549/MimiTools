using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;
using MimiTools.CodeBuilder.Code;
using System.Collections.Generic;
using System.Linq;

namespace MimiTools.CodeBuilder
{
    public class MethodBuilder : NodeBuilder
    {
        public Accessibility Accessibility { get; set; }

        public List<Statement> Body { get; set; }

        public DeclarationModifiers DeclarationModifiers { get; set; }

        public string Name { get; set; }

        public List<ParameterBuilder> Parameters { get; set; } = new List<ParameterBuilder>();

        public ReferenceBuilder ReturnType { get; set; }

        public List<TypeParameterConfig> TypeParameters { get; set; } = new List<TypeParameterConfig>();

        private protected override SyntaxNode Build(SyntaxGenerator generator)
            => generator.MethodDeclaration(
                Name,
                BuildMany(generator, Parameters),
                TypeParameters.Select(p => p.Name),
                ReturnType.BuildInternal(generator),
                Accessibility,
                DeclarationModifiers,
                BuildMany(generator, Body));

        public override void Clear()
        {
            Accessibility = default;

            if (Body == null)
                Body = new List<Statement>();
            else
                Body.Clear();

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
    }
}

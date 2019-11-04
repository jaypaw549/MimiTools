using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;
using System.Collections.Generic;

namespace MimiTools.CodeBuilder
{
    public sealed class TypeReferenceBuilder : ReferenceBuilder
    {
        public string Name { get; set; }

        public bool IsDotted { get; set; }

        private readonly List<ReferenceBuilder> _parameters = new List<ReferenceBuilder>();
        public ReferenceBuilder AddParameter(ReferenceBuilder parameter)
        {
            _parameters.Add(parameter);
            return this;
        }

        private protected override SyntaxNode Build(SyntaxGenerator generator)
        {
            if (_parameters.Count > 0)
                return generator.GenericName(Name, BuildMany(generator, _parameters));

            if (IsDotted)
                return generator.DottedName(Name);

            return generator.IdentifierName(Name);
        }

        public override void Clear()
        {
            Name = null;
            IsDotted = default;
            _parameters.Clear();
        }

        public ReferenceBuilder SetDotted(bool yes)
        {
            IsDotted = yes;
            return this;
        }

        public ReferenceBuilder SetName(string name)
        {
            Name = name;
            return this;
        }
    }
}

using Microsoft.CodeAnalysis.Editing;
using System.Collections.Generic;

namespace MimiTools.CodeBuilder
{
    public sealed class TypeParameterConfig
    {
        public string Name { get; set; } = string.Empty;

        public SpecialTypeConstraintKind SpecialTypeConstraintKind { get; set; } = SpecialTypeConstraintKind.None;

        public List<ReferenceBuilder> TypeConstraints { get; set; } = new List<ReferenceBuilder>();

        public TypeParameterConfig AddTypeConstraint(ReferenceBuilder reference)
        {
            if (TypeConstraints == null)
                TypeConstraints = new List<ReferenceBuilder>();

            TypeConstraints.Add(reference);

            return this;
        }

        public TypeParameterConfig SetName(string name)
        {
            Name = name;
            return this;
        }

        public TypeParameterConfig SetSpecialTypeConstraintKind(SpecialTypeConstraintKind kind)
        {
            SpecialTypeConstraintKind = kind;
            return this;
        }
    }
}
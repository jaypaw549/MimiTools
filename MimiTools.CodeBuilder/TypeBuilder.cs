using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;
using System.Collections.Generic;
using System.Linq;

namespace MimiTools.CodeBuilder
{
    public abstract class TypeBuilder<BuilderType> : NodeBuilder where BuilderType : TypeBuilder<BuilderType>
    {
        internal TypeBuilder() { }

        public Accessibility Accessibility { get; set; }

        public DeclarationModifiers DeclarationModifiers { get; set; }

        public List<ReferenceBuilder> Interfaces { get; set; } = new List<ReferenceBuilder>();
        public string Name { get; set; }
        public List<TypeParameterConfig> TypeParameters { get; set; } = new List<TypeParameterConfig>();

        private protected readonly List<NodeBuilder> _members = new List<NodeBuilder>();

        public BuilderType AddInterface(ReferenceBuilder reference)
        {
            if (Interfaces == null)
                Interfaces = new List<ReferenceBuilder>();

            Interfaces.Add(reference);
            return (BuilderType)this;
        }

        public BuilderType AddInterface(string name, bool is_dotted)
        {
            if (Interfaces == null)
                Interfaces = new List<ReferenceBuilder>();

            Interfaces.Add(new TypeReferenceBuilder() { Name = name, IsDotted = is_dotted });
            return (BuilderType)this;
        }

        public BuilderType AddTypeParameter(TypeParameterConfig info)
        {
            if (TypeParameters == null)
                TypeParameters = new List<TypeParameterConfig>();

            TypeParameters.Add(info);
            return (BuilderType)this;
        }

        public BuilderType AddTypeParameter(string name, SpecialTypeConstraintKind special_constraints, List<ReferenceBuilder> type_constraints)
        {
            if (TypeParameters == null)
                TypeParameters = new List<TypeParameterConfig>();

            TypeParameters.Add(new TypeParameterConfig()
            {
                Name = name,
                SpecialTypeConstraintKind = special_constraints,
                TypeConstraints = type_constraints
            });
            return (BuilderType)this;
        }

        private protected IEnumerable<SyntaxNode> BuildInterfaces(SyntaxGenerator generator)
            => BuildMany(generator, Interfaces);

        private protected IEnumerable<SyntaxNode> BuildMembers(SyntaxGenerator generator)
            => BuildMany(generator, _members);

        public override void Clear()
        {
            Accessibility = default;
            DeclarationModifiers = default;

            if (Interfaces == null)
                Interfaces = new List<ReferenceBuilder>();
            else
                Interfaces.Clear();

            Name = default;

            if (TypeParameters == null)
                TypeParameters = new List<TypeParameterConfig>();
            else
                TypeParameters.Clear();

            _members.Clear();
        }

        private protected SyntaxNode ConstrainTypeParameters(SyntaxNode node, SyntaxGenerator generator)
        {
            foreach (TypeParameterConfig config in TypeParameters)
                node = generator.WithTypeConstraint(node, config.Name, config.SpecialTypeConstraintKind, BuildMany(generator, config.TypeConstraints));

            return node;
        }

        private protected IEnumerable<string> GetTypeParameterNames()
            => TypeParameters.Select(i => i.Name);

        public BuilderType SetAccessibility(Accessibility accessibility)
        {
            Accessibility = accessibility;
            return (BuilderType)this;
        }

        public BuilderType SetDeclarationModifiers(DeclarationModifiers modifiers)
        {
            DeclarationModifiers = modifiers;
            return (BuilderType)this;
        }

        public BuilderType SetName(string name)
        {
            Name = name;
            return (BuilderType)this;
        }
    }
}
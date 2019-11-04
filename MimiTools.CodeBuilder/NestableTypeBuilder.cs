namespace MimiTools.CodeBuilder
{
    public abstract class NestableTypeBuilder<BuilderType> : TypeBuilder<BuilderType> where BuilderType : NestableTypeBuilder<BuilderType>
    {
        private protected NestableTypeBuilder() { }
        public BuilderType AddInnerClass(ClassBuilder builder)
        {
            _members.Add(builder);
            return (BuilderType)this;
        }

        public BuilderType AddInnerEnum(EnumBuilder builder)
        {
            _members.Add(builder);
            return (BuilderType)this;
        }

        public BuilderType AddInnerInterface(InterfaceBuilder builder)
        {
            _members.Add(builder);
            return (BuilderType)this;
        }

        public BuilderType AddInnerStruct(StructBuilder builder)
        {
            _members.Add(builder);
            return (BuilderType)this;
        }

        public BuilderType AddField(FieldBuilder builder)
        {
            _members.Add(builder);
            return (BuilderType)this;
        }
    }
}

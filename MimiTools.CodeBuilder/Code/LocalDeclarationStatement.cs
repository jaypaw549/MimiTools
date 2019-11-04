namespace MimiTools.CodeBuilder.Code
{
    internal abstract class LocalDeclarationStatement : Statement
    {
        internal LocalDeclarationStatement(string name, Expression initializer)
        {
            this.name = name;
            this.initializer = initializer;
        }

        protected readonly string name;
        protected readonly Expression initializer;
    }
}

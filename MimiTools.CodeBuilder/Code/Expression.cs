namespace MimiTools.CodeBuilder.Code
{
    public abstract class Expression : NodeBuilder
    {
        private protected Expression() { }

        public override void Clear()
            => throw new System.InvalidOperationException("Type expression is immutable!");

        public Statement ToStatement()
            => new ExpressionStatement(this);
    }
}

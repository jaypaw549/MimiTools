namespace MimiTools.Expressions.Components
{
    public abstract class Expression<TOut> : IExpression<TOut>
    {
        public abstract TOut Execute(Environment env);

        public abstract string ToCode();

        object IExpression.Execute(Environment env)
            => Execute(env);
    }
}

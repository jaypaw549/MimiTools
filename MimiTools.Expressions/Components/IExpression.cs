using Env = MimiTools.Expressions.Components.Environment;

namespace MimiTools.Expressions.Components
{
    public interface IExpression
    {
        object Execute(Env env);

        string ToCode();
    }

    public interface IExpression<out TOut> : IExpression
    {
        new TOut Execute(Env env);
    }
}

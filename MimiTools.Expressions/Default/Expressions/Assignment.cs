using MimiTools.Expressions.Components;
using System.Text.RegularExpressions;
using static MimiTools.Expressions.Default.RegexStorage;

namespace MimiTools.Expressions.Default
{
    public class Assignment : ExpressionReader
    {
        private const string target = "Target";
        private const string value = "Value";

        private static readonly Regex reader = new Regex($@"^(?<{target}>(?>({handle_parenthesis}|[^""()=]))+)=(?<{value}>(?>({handle_parenthesis}|[^""()=]))+)$",
            RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.Singleline);

        public override bool TryReadExpression(CodeReader reader, CodeSection code, out IExpression e)
        {
            Match m = Assignment.reader.Match(code.ToString());
            e = null;
            if (m.Success)
            {
                Group g = m.Groups[target];
                if (!reader.TryReadExpression(code.CreateSubsection(g.Index, g.Length), out IExpression target_expression))
                    return false;

                g = m.Groups[value];
                if (!reader.TryReadExpression(code.CreateSubsection(g.Index, g.Length), out IExpression value_expression))
                    return false;

                if (target_expression is IExpression<Variable> var_expression)
                {
                    e = new AssignmentExpression(var_expression, value_expression);
                    return true;
                }
            }
            return false;
        }
        private class AssignmentExpression : IExpression
        {
            private readonly IExpression<Variable> target;
            private readonly IExpression value;

            internal AssignmentExpression(IExpression<Variable> target, IExpression value)
            {
                this.target = target;
                this.value = value;
            }

            object IExpression.Execute(Components.Environment env)
            {
                Variable var = target.Execute(env);
                if (env.Exception != null)
                    return null;

                object value = this.value.Execute(env);

                if (env.Exception != null)
                    return null;



                return value;
            }

            string IExpression.ToCode()
                => $"{target.ToCode()} = {value.ToCode()}";
        }
    }
}

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;

namespace MimiTools.CodeBuilder.Code
{
    internal sealed class ExitSwitchStatement : Statement
    {
        internal static ExitSwitchStatement Instance { get; } = new ExitSwitchStatement();

        internal ExitSwitchStatement()
        {
        }

        private protected override SyntaxNode Build(SyntaxGenerator generator)
            => generator.ExitSwitchStatement();
    }
}

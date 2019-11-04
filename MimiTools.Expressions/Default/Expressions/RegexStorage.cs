namespace MimiTools.Expressions.Default
{
    internal static class RegexStorage
    {
        internal const string handle_parenthesis = @"(?ns:(?>(?>(?<pon>\()?(?(pon)[^()""]*)(?<poff-pon>\))?)*(?>""(?>\\.|[^""])*"")*)*(?(pon)(?!)))";
        internal const string handle_scope = @"(?ns:(?>(?>(?<son>\{)?(?(son)[^()""]*)(?<soff-son>\})?)*(?>""(?>\\.|[^""])*"")*)*(?(son)(?!)))";
    }
}

using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace MimiTools.Tools
{
    public class StringEscaper
    {
        private readonly Dictionary<string, string> LiteralsToEscapes = new Dictionary<string, string>();

        private readonly Dictionary<string, string> EscapesToLiterals = new Dictionary<string, string>();

        private string EscapePattern { get => $@"({string.Join("|", LiteralsToEscapes.Keys.Select(s => Regex.Escape(s)))})"; }

        private string UnescapePattern { get => $@"({string.Join("|", EscapesToLiterals.Keys.Select(s => Regex.Escape(s)))})"; }

        public StringEscaper(IEnumerable<KeyValuePair<string, string>> literalsToEscapes)
        {
            foreach (KeyValuePair<string, string> kvp in literalsToEscapes)
            {
                LiteralsToEscapes[kvp.Key] = kvp.Value;
                EscapesToLiterals[kvp.Value] = kvp.Key;
            }
        }

        public string EscapeString(string s)
            => Regex.Replace(s, EscapePattern, m => LiteralsToEscapes[m.Value]);

        public string UnescapeString(string s)
            => Regex.Replace(s, UnescapePattern, m => EscapesToLiterals[m.Value]);
    }
}

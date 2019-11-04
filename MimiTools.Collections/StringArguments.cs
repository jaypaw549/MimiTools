using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace MimiTools.Collections
{
    public class StringArguments : IEnumerable<string>, IReadOnlyList<string>
    {
        internal static readonly StringArguments Empty;

        static StringArguments()
        {
            ArgumentsParser = new Regex(@"(?<!\S)((?<quotes>[""]).*?\k<quotes>|\S)+?(?!\S)", RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.Singleline);
            Dequoter = new Regex(@"(?<=(?<quote>[""])).*(?=\k<quote>)", RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.Singleline);
            Empty = new StringArguments(string.Empty);
        }

        public static string Dequote(string text)
        {
            Match m = Dequoter.Match(text);
            if (m.Success)
                text = m.Value;
            return text;
        }

        private readonly string Arguments;
        private readonly MatchCollection Collection;

        public StringArguments(string args)
        {
            Arguments = args;
            Collection = ArgumentsParser.Matches(args);
        }

        public static readonly Regex ArgumentsParser;
        public static readonly Regex Dequoter;

        public int Count => Collection.Count;

        public string this[int index] => Collection[index].Value;

        public string GetAsRemaining(int index)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));

            if (index == 0)
                return Arguments;

            Match m = Collection[index - 1];
            return Arguments.Substring(m.Index + m.Length);
        }

        public ArgumentsEnumerator GetEnumerator()
            => new ArgumentsEnumerator(Arguments);

        IEnumerator<string> IEnumerable<string>.GetEnumerator()
            => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();

        public string[] ToArray(int count)
        {
            if (count > Count || count <= 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            string[] array = new string[count];

            count--;

            for (int i = 0; i < count; i++)
                array[i] = this[i];

            array[count] = GetAsRemaining(count);

            return array;
        }

        public class ArgumentsEnumerator : IEnumerator<string>
        {
            internal ArgumentsEnumerator(string args)
                => Remaining = args;

            public string Current { get; private set; }

            private bool Disposed = false;

            private string Remaining;

            object IEnumerator.Current => Current;

            public void Dispose()
            {
                Current = null;
                Disposed = true;
                Remaining = null;
            }

            public string GetRemaining(bool consume = false)
            {
                string ret = Remaining.Trim();
                if (consume)
                {
                    Current = string.Empty;
                    Remaining = string.Empty;
                }
                return ret;
            }

            public bool MoveNext()
            {
                if (Disposed)
                    throw new InvalidOperationException("Disposed.");

                Match match = ArgumentsParser.Match(Remaining);
                Current = match.Value;

                if (match.Success)
                {
                    Remaining = Remaining.Remove(0, match.Index + match.Length);
                    return true;
                }

                Remaining = string.Empty;
                return false;
            }

            public void Reset()
                => throw new NotSupportedException();
        }
    }
}

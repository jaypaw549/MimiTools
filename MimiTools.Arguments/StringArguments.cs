using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace MimiTools.Arguments
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

            if (index == Collection.Count)
                return string.Empty;

            Match m = Collection[index];
            Match l = Collection[Collection.Count - 1];
            return Arguments.Substring(m.Index, l.Index + l.Length - m.Index);
        }

        public ArgumentsEnumerator GetEnumerator()
            => new ArgumentsEnumerator(this);

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
            internal ArgumentsEnumerator(StringArguments args)
            {
                Arguments = args;
                Disposed = false;
                Index = -1;
            }

            public string Current { get => Disposed ? throw new InvalidOperationException("Disposed!") : Arguments[Index]; }

            private readonly StringArguments Arguments;
            private bool Disposed;
            private int Index;

            object IEnumerator.Current => Current;

            public void Dispose()
            {
                Disposed = true;
            }

            public string GetRemaining(bool consume = false)
            {
                if (Index >= Arguments.Count)
                    return string.Empty;

                string ret = Arguments.GetAsRemaining(Index);
                if (consume)
                    Index = Arguments.Count;
                
                return ret;
            }

            public bool MoveNext()
            {
                if (Disposed)
                    throw new InvalidOperationException("Disposed.");

                return ++Index < Arguments.Count;
            }

            public void Reset()
                => throw new NotSupportedException();
        }
    }
}

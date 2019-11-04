using System;

namespace MimiTools.Expressions
{
    public struct CodeSection
    {
        private CodeSection(string code, int start, int length)
        {
            this.code = code;
            this.start = start;
            Length = length;
        }

        public char this[int index]
        {
            get
            {
                CheckVars(index, null);
                return code[start + index];
            }
        }

        public int Length { get; }

        private readonly string code;
        private readonly int start;

        public CodeSection CreateSubsection(int start, int length)
        {
            CheckVars(start, length);
            return new CodeSection(code, this.start + start, Math.Min(this.Length - start, length));
        }

        private void CheckVars(int start, int? length)
        {
            if (start >= this.start + Length)
                throw new IndexOutOfRangeException(nameof(start));

            if (length.HasValue && start + length.Value > Length)
                throw new IndexOutOfRangeException(nameof(length));
        }

        internal static CodeSection FromString(string code)
            => new CodeSection(code, 0, code.Length);

        public override string ToString()
            => code.Substring(start, Length);
    }
}

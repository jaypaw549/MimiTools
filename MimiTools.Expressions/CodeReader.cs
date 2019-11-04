using MimiTools.Expressions.Components;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MimiTools.Expressions
{
    public sealed class CodeReader
    {
        private readonly ExpressionReader[] readers;
        public CodeReader(params ExpressionReader[] readers)
        {
            this.readers = readers;
        }

        public CodeReader(IEnumerable<ExpressionReader> readers)
        {
            this.readers = readers.ToArray();
        }

        public IExpression ReadExpression(string expression)
        {
            CodeSection section = CodeSection.FromString(expression);
            if (!TryReadExpression(section, out IExpression e))
                throw new FormatException("Unable to read expression!");

            return e;
        }

        public bool TryReadExpression(CodeSection section, out IExpression e)
        {
            foreach (ExpressionReader reader in readers)
                if (reader.TryReadExpression(this, section, out e))
                    return true;

            e = null;
            return false;
        }
    }
}

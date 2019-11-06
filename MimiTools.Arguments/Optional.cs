using System;
using System.Collections.Generic;
using System.Text;

namespace MimiTools.Arguments
{
    internal struct Optional<T>
    {
        internal Optional(T value)
        {
            IsSpecified = true;
            Value = value;
        }

        public bool IsSpecified { get; }
        public T Value { get; }
    }
}

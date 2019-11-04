using MimiTools.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MimiTools.Arguments
{
    public partial class ArgumentsParser<T> where T : new()
    {
        private readonly Dictionary<string, ArgumentsContainer> Setters = new Dictionary<string, ArgumentsContainer>();

        public T Parse(string args)
            => Parse(new StringArguments(args));

        public T Parse(IEnumerable<string> args)
        {



            return new T();
        }

        private class ArgumentsContainer
        {
            internal Func<string, object> Converters;
            internal Action<object[]> Set;
        }
    }
}

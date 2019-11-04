using System.Collections.Generic;

namespace MimiTools.Expressions.Components
{
    public class Environment
    {
        private readonly Dictionary<string, object> variables;

        public object Exception { get; }

        internal object GetValue(string name)
            => variables[name];

        internal Variable GetOrDefineVariable(string name)
        {
            if (!variables.ContainsKey(name))
                variables[name] = null;

            return new Variable(name, this);
        }
    }
}

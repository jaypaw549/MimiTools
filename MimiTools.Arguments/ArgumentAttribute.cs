using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MimiTools.Arguments
{
    public class ArgumentAttribute : Attribute
    {
        public string Name { get; }
        public IReadOnlyCollection<string> Aliases { get; }

        public ArgumentAttribute(string name, params string[] aliases)
        {
            Name = name;
            Aliases = Array.AsReadOnly(aliases ?? new string[0]);
        }
    }
}

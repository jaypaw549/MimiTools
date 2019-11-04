using System;
using System.Collections.Generic;
using System.Text;

namespace MimiTools.Loader
{
    internal struct TypeRef
    {
        public TypeRef(string name, string space)
        {
            Name = name;
            Namespace = space;
        }

        public readonly string Name;
        public readonly string Namespace;
    }
}

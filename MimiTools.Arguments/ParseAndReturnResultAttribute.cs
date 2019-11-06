using System;
using System.Collections.Generic;
using System.Text;

namespace MimiTools.Arguments
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class ParseAndReturnResultAttribute : Attribute
    {
        public Type ParseAs { get; set; }
    }
}

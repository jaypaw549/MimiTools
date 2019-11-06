using System;
using System.Collections.Generic;
using System.Text;

namespace MimiTools.Arguments
{
    /// <summary>
    /// Specifies an argument which is interpreted in a specific order
    /// </summary>
    [AttributeUsage(ArgumentsParser._usage, AllowMultiple = false)]
    public class PositionArgumentAttribute : Attribute
    {
        /// <summary>
        /// If true, allows flags to be parsed in this position. Is true by default if position = 0
        /// </summary>
        public bool AllowFlags { get; set; }

        public int Position { get; }

        public int Priority { get; set; }

        public PositionArgumentAttribute(int position)
        {
            AllowFlags = true;
            Position = position;
            Priority = 0;
        }
    }
}

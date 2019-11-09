using System;
using System.Collections.Generic;
using System.Text;

namespace MimiTools.Arguments
{
    /// <summary>
    /// Defines a field or property that is assigned when the specified flag is present,
    /// or defines a method that is called when the flag is present.
    /// 
    /// if "-f" or "--force" is present when parsing the arguments, this method is called:
    /// <code>
    /// [FlagArgument("--force")]
    /// [FlagArgument("-f")]
    /// public void Force()
    /// {
    ///     //Do stuff to toggle the force flag as on
    /// }
    /// </code>    
    /// "-p 22" in the input will assign 22 to this property
    /// <code>
    /// [FlagArgument("-p")]
    /// public int Port { get; set; }
    /// </code>
    /// </summary>
    [AttributeUsage(ArgumentsManager._usage, AllowMultiple = true)]
    public class FlagArgumentAttribute : Attribute
    {
        public string Flag { get; }

        public int Priority { get; set; }

        public FlagArgumentAttribute(string flag)
        {
            Flag = flag;
            Priority = 0;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace MimiTools.Arguments
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Property, AllowMultiple = false)]
    public class SubParseAttribute : Attribute
    {
    }
}

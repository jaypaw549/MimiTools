using System;
using System.Collections.Generic;
using System.Text;

namespace MimiTools.Arguments
{
    public interface IArgumentConverter
    {
        ConversionCompability GetCompatibilty(Type t);

        bool TryConvert(string data, Type type, out object obj);
    }
}

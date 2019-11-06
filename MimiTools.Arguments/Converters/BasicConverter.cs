using System;
using System.Collections.Generic;
using System.Text;

namespace MimiTools.Arguments.Converters
{
    class BasicConverter : IArgumentConverter
    {
        internal static IArgumentConverter Instance { get; } = new BasicConverter();

        public ConversionCompability GetCompatibilty(Type t)
            => ConversionCompability.Unknown;

        public bool TryConvert(string data, Type type, out object obj)
        {
            try
            {
                obj = Convert.ChangeType(data, type);
                return true;
            }
            catch
            {
                obj = null;
                return false;
            }
        }
    }
}

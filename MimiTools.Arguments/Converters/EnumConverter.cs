using System;
using System.Collections.Generic;
using System.Text;

namespace MimiTools.Arguments.Converters
{
    class EnumConverter : IArgumentConverter
    {
        internal static IArgumentConverter Instance { get; } = new EnumConverter();

        private readonly Dictionary<Type, Dictionary<string, object>> _values = new Dictionary<Type, Dictionary<string, object>>();
        public ConversionCompability GetCompatibilty(Type t)
            => t.IsEnum ? ConversionCompability.Possible : ConversionCompability.Impossible;

        public bool TryConvert(string data, Type type, out object obj)
        {
            if(!_values.TryGetValue(type, out Dictionary<string, object> names))
            {
                names = new Dictionary<string, object>();
                foreach (object o in Enum.GetValues(type))
                    names.Add(Enum.GetName(type, o), o);
                _values[type] = names;
            }

            return names.TryGetValue(data, out obj);
        }
    }
}

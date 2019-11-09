using System;
using System.Collections.Generic;
using System.Text;

namespace MimiTools.Arguments.Converters
{
    class NullableConverter : IArgumentConverter
    {
        private readonly ArgumentsManager _manager;

        internal NullableConverter(ArgumentsManager manager)
            => _manager = manager;

        public ConversionCompability GetCompatibilty(Type t)
            => t.IsGenericType && !t.ContainsGenericParameters && t.GetGenericTypeDefinition() == typeof(Nullable<>) ? ConversionCompability.Possible : ConversionCompability.Impossible;

        public bool TryConvert(string data, Type type, out object obj)
            => _manager.TryConvert(data, type.GetGenericArguments()[0], out obj);
    }
}

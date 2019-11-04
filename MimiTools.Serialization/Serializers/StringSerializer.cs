using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MimiTools.Serialization.Serializers
{
    public class StringSerializer : ISerializer
    {
        public IEnumerable<Type> DeserializableTypes => new Type[] { typeof(string) };

        public IEnumerable<Type> SerializableTypes => new Type[] { typeof(string) };

        public SerializationStatus Deserialize(SerializationInstance instance, Type t, SerializationInfo info, StreamingContext context)
            => SerializationStatus.Deserialized(info.GetString("s_value"));

        public SerializationStatus Serialize(SerializationInstance instance, object obj, SerializationInfo info, StreamingContext context)
        {
            info.AddValue("s_value", (string)obj, typeof(string));
            return SerializationStatus.Success;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MimiTools.Serialization.Serializers
{
    public class GuidSerializer : ISerializer
    {
        public IEnumerable<Type> DeserializableTypes => new Type[] { typeof(Guid) };

        public IEnumerable<Type> SerializableTypes => new Type[] { typeof(Guid) };

        public SerializationStatus Deserialize(SerializationInstance instance, Type t, SerializationInfo info, StreamingContext context)
            => SerializationStatus.Deserialized(Guid.Parse(info.GetString("value")));

        public SerializationStatus Serialize(SerializationInstance instance, object obj, SerializationInfo info, StreamingContext context)
        {
            info.AddValue("value", ((Guid)obj).ToString());
            return SerializationStatus.Success;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MimiTools.Serialization
{
    public interface ISerializer
    {
        IEnumerable<Type> DeserializableTypes { get; }
        IEnumerable<Type> SerializableTypes { get; }

        SerializationStatus Deserialize(SerializationInstance instance, Type t, SerializationInfo info, StreamingContext context);

        SerializationStatus Serialize(SerializationInstance instance, object obj, SerializationInfo info, StreamingContext context);
    }
}

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MimiTools.Serialization.Serializers
{
    public sealed class StubSerializer : ISerializer
    {
        public static readonly StubSerializer Instance = new StubSerializer();

        public IEnumerable<Type> DeserializableTypes => new Type[0];

        public IEnumerable<Type> SerializableTypes => new Type[0];

        public SerializationStatus Deserialize(SerializationInstance instance, Type t, SerializationInfo info, StreamingContext context)
            => SerializationStatus.Failure;

        public SerializationStatus Serialize(SerializationInstance instance, object obj, SerializationInfo info, StreamingContext context)
            => SerializationStatus.Failure;
    }
}

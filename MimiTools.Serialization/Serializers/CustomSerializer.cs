using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MimiTools.Serialization.Serializers
{
    public class CustomSerializer : ISerializer
    {
        public CustomSerializer(CustomSerializerDelegate serializer, CustomDeserializerDelegate deserializer, bool deep, params Type[] types)
        {
            Type[] t = new Type[types.Length];
            types.CopyTo(t, 0);

            Deep = deep;

            Serializer = serializer;
            if (Serializer != null)
                SerializableTypes = t;
            else
                SerializableTypes = Type.EmptyTypes;

            Deserializer = deserializer;
            if (Deserializer != null)
                DeserializableTypes = t;
            else
                DeserializableTypes = Type.EmptyTypes;
        }

        private readonly CustomSerializerDelegate Serializer;
        private readonly CustomDeserializerDelegate Deserializer;
        private readonly bool Deep;

        public IEnumerable<Type> DeserializableTypes { get; }

        public IEnumerable<Type> SerializableTypes { get; }

        public SerializationStatus Deserialize(SerializationInstance instance, Type t, SerializationInfo info, StreamingContext context)
        {
            if (Deserializer == null || !Deserializer(Deep ? instance.CreateLoopbackInfo(info) : info, context, out object value))
                return SerializationStatus.Failure;

            return SerializationStatus.Deserialized(value);
        }

        public SerializationStatus Serialize(SerializationInstance instance, object obj, SerializationInfo info, StreamingContext context)
        {
            SerializationInfo proxy = Deep ? new SerializationInfo(obj.GetType(), new FormatterConverter()) : null;
            if (Serializer == null || !Serializer(Deep ? proxy : info, context, obj))
                return SerializationStatus.Failure;

            if (Deep)
                instance.CopyAndSerialize(proxy, info);
            return SerializationStatus.Success;
        }
    }

    /// <summary>
    /// A delegate that performs a deserialization task
    /// </summary>
    /// <param name="info">The object info to deserialize from</param>
    /// <param name="context">The context we're pulling from</param>
    /// <param name="value">The deserialized object</param>
    /// <returns>Whether or not deserialization was successful</returns>
    public delegate bool CustomDeserializerDelegate(SerializationInfo info, StreamingContext context, out object value);

    /// <summary>
    /// A delegate that performs a serialization task
    /// </summary>
    /// <param name="info">The object info to serialize to</param>
    /// <param name="context">The context we're going to store the object in</param>
    /// <param name="obj">The object to serialize</param>
    /// <returns>Whether or not the serialization was successful</returns>
    public delegate bool CustomSerializerDelegate(SerializationInfo info, StreamingContext context, object obj);
}

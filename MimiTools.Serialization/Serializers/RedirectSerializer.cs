using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MimiTools.Serialization.Serializers
{
    public class RedirectSerializer : ISerializer
    {
        public RedirectSerializer(Type redirect_from, Type redirect_to, bool deserialization_only)
        {
            DeserializableTypes = new Type[] { redirect_from };
            if (!deserialization_only)
                SerializableTypes = new Type[] { redirect_from };
            else
                SerializableTypes = Type.EmptyTypes;
            RedirectTo = redirect_to;
        }

        private readonly Type RedirectTo;

        public IEnumerable<Type> DeserializableTypes { get; }

        public IEnumerable<Type> SerializableTypes { get; }

        public SerializationStatus Deserialize(SerializationInstance instance, Type t, SerializationInfo info, StreamingContext context)
            => SerializationStatus.UseAlternativeType(RedirectTo);

        public SerializationStatus Serialize(SerializationInstance instance, object obj, SerializationInfo info, StreamingContext context)
            => SerializationStatus.UseAlternativeType(RedirectTo);
    }
}

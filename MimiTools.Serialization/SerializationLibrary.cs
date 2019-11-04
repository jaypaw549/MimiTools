using MimiTools.Serialization.Serializers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace MimiTools.Serialization
{
    public sealed class SerializationLibrary
    {
        public static SerializationLibrary Standard { get; } =
            new SerializationLibrary(

                //Optimizing Serializers
                new GuidSerializer(),

                //Workaround Serializers
                new ArraySerializer(),
                new DictionarySerializer(),
                new StringSerializer(),

                //Default Serializers
                new DeepSerializer(),
                new ShallowDeserializer()
                );

        public readonly SerializationLibrary BaseLibrary;
        public readonly bool ReadOnly;

        private readonly List<ISerializer> DefaultSerializers;
        private readonly HashSet<ISerializer> Serializers;
        private readonly Dictionary<Type, ISerializer> TypeDeserializers;
        private readonly Dictionary<Type, ISerializer> TypeSerializers;

        public SerializationLibrary(SerializationLibrary base_lib)
        {
            BaseLibrary = base_lib;
            ReadOnly = false;


            DefaultSerializers = new List<ISerializer>();
            Serializers = new HashSet<ISerializer>();
            TypeDeserializers = new Dictionary<Type, ISerializer>();
            TypeSerializers = new Dictionary<Type, ISerializer>();
        }

        private SerializationLibrary(SerializationLibrary lib, bool read)
        {
            DefaultSerializers = new List<ISerializer>(lib.DefaultSerializers);

            Serializers = new HashSet<ISerializer>(lib.Serializers);

            if (ReadOnly = read)
                Serializers.TrimExcess();

            TypeDeserializers = new Dictionary<Type, ISerializer>(lib.TypeDeserializers);
            TypeSerializers = new Dictionary<Type, ISerializer>(lib.TypeSerializers);

            if (!lib.BaseLibrary?.ReadOnly ?? false)
                BaseLibrary = new SerializationLibrary(lib.BaseLibrary, true);
            else
                BaseLibrary = lib.BaseLibrary;
        }

        private SerializationLibrary(params ISerializer[] serializers)
        {
            ReadOnly = true;

            DefaultSerializers = new List<ISerializer>();
            Serializers = new HashSet<ISerializer>();
            TypeDeserializers = new Dictionary<Type, ISerializer>();
            TypeSerializers = new Dictionary<Type, ISerializer>();

            foreach (ISerializer serializer in serializers)
                AddSerializerInternal(serializer);

            Serializers.TrimExcess();
        }

        public SerializationLibrary AddSerializer(ISerializer serializer)
        {
            if (ReadOnly)
                throw new InvalidOperationException("This library is read-only!");

            if (serializer == null)
                throw new ArgumentNullException(nameof(serializer));

            AddSerializerInternal(serializer);

            return this;
        }

        private void AddSerializerInternal(ISerializer serializer)
        {
            if (!Serializers.Add(serializer))
                return;

            if (serializer.DeserializableTypes != null)
                foreach (Type t in serializer.DeserializableTypes.Where(t => !TypeDeserializers.ContainsKey(t)))
                    TypeDeserializers.Add(t, serializer);

            if (serializer.SerializableTypes != null)
                foreach (Type t in serializer.SerializableTypes.Where(t => !TypeSerializers.ContainsKey(t)))
                    TypeSerializers.Add(t, serializer);

            if (serializer.DeserializableTypes == null && serializer.SerializableTypes == null)
                DefaultSerializers.Add(serializer);
        }

        public SerializationLibrary Copy(bool writeable)
            => new SerializationLibrary(this, !writeable);

        public object Deserialize(Type t, SerializedObject obj, StreamingContext context = default(StreamingContext))
            => new SerializationInstance(this, context).StartDeserialize(t, obj);

        internal IEnumerable<ISerializer> GetDeserializers(Type t)
            => GetDeserializers(t, true, true);

        private IEnumerable<ISerializer> GetDeserializers(Type t, bool interfaces, bool base_types)
        {
            if (TypeDeserializers.TryGetValue(t, out ISerializer serializer))
                yield return serializer;

            if (t.IsConstructedGenericType)
                foreach (ISerializer s in GetDeserializers(t.GetGenericTypeDefinition(), false, false))
                    yield return s;

            if (base_types && t.BaseType != null)
                foreach (ISerializer s in GetDeserializers(t.BaseType, false, true))
                    yield return s;

            if (interfaces)
                foreach (ISerializer s in t.GetInterfaces().SelectMany(i => GetDeserializers(i, false, false)))
                    yield return s;

            foreach (ISerializer s in DefaultSerializers)
                yield return s;
        }

        internal IEnumerable<ISerializer> GetSerializers(Type t)
            => GetSerializers(t, true, true);

        private IEnumerable<ISerializer> GetSerializers(Type t, bool interfaces, bool base_types)
        {
            if (TypeSerializers.TryGetValue(t, out ISerializer serializer))
                yield return serializer;

            if (t.IsConstructedGenericType)
                foreach (ISerializer s in GetSerializers(t.GetGenericTypeDefinition(), false, false))
                    yield return s;

            if (base_types && t.BaseType != null)
                foreach (ISerializer s in GetSerializers(t.BaseType, false, true))
                    yield return s;

            if (interfaces)
                foreach (ISerializer s in t.GetInterfaces().SelectMany(i => GetSerializers(i, false, false)))
                    yield return s;

            foreach (ISerializer s in DefaultSerializers)
                yield return s;

            if (BaseLibrary != null)
                foreach (ISerializer s in BaseLibrary.GetSerializers(t))
                    yield return s;
        }

        public SerializedObject Serialize(object obj, StreamingContext context = default(StreamingContext))
            => new SerializationInstance(this, context).Serialize(obj);

        public SerializationLibrary ToReadOnly()
        {
            if (ReadOnly)
                return this;

            return new SerializationLibrary(this, true);
        }
    }
}

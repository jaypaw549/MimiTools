using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MimiTools.Serialization.Serializers
{
    public class ArraySerializer : ISerializer
    {
        public IEnumerable<Type> DeserializableTypes => new Type[] { typeof(Array) };

        public IEnumerable<Type> SerializableTypes => new Type[] { typeof(Array) };

        private SerializationFixup CreateArrayFixup(int[] missing_indexes)
        {
            return fixup;
            SerializationStatus fixup(SerializationInstance instance, object obj, ulong[] ids, object[] values)
            {
                Array array = (Array)obj;
                for (int i = 0; i < missing_indexes.Length; i++)
                    array.SetValue(values[i], missing_indexes[i]);
                return SerializationStatus.Success;
            }
        }

        public SerializationStatus Deserialize(SerializationInstance instance, Type t, SerializationInfo info, StreamingContext context)
        {
            if (!t.IsArray)
                return SerializationStatus.Failure;

            if (t == typeof(Array))
                return SerializationStatus.Failure;

            SerializedObject[] serialized_array = (SerializedObject[])info.GetValue("array", typeof(SerializedObject[]));

            Type e_type = t.GetElementType();

            Array array = Array.CreateInstance(e_type, serialized_array.Length);

            List<int> missing_indexes = new List<int>();
            List<ulong> missing_ids = new List<ulong>();

            for (int i = 0; i < serialized_array.Length; i++)
            {
                if (!instance.TryGetOrDeserialize(e_type, serialized_array[i], out object value))
                {
                    missing_ids.Add(serialized_array[i].Id);
                    missing_indexes.Add(i);
                    continue;
                }

                array.SetValue(value, i);
            }

            if (missing_indexes.Count > 0)
                return SerializationStatus.FixupLater(array, CreateArrayFixup(missing_indexes.ToArray()), missing_ids.ToArray());

            return SerializationStatus.Deserialized(array);
        }

        public SerializationStatus Serialize(SerializationInstance instance, object obj, SerializationInfo info, StreamingContext context)
        {
            if (!(obj is Array array))
                return SerializationStatus.Failure;

            SerializedObject[] serialized_array = new SerializedObject[array.Length];

            for (int i = 0; i < array.Length; i++)
                serialized_array[i] = instance.Serialize(array.GetValue(i));

            info.AddValue("array", serialized_array);

            return SerializationStatus.Success;
        }
    }
}

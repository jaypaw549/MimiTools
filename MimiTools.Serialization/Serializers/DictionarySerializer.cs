using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MimiTools.Serialization.Serializers
{
    public class DictionarySerializer : ISerializer
    {
        public IEnumerable<Type> DeserializableTypes => new Type[] { typeof(Dictionary<,>) };

        public IEnumerable<Type> SerializableTypes => new Type[] { typeof(Dictionary<,>) };

        public SerializationStatus Deserialize(SerializationInstance instance, Type t, SerializationInfo info, StreamingContext context)
        {
            Type[] args = t.GetGenericArguments();
            Type key_array_type = args[0].MakeArrayType();
            Type value_array_type = args[1].MakeArrayType();

            object o = Activator.CreateInstance(t);

            SerializedObject serialized_keys = (SerializedObject)info.GetValue("keys", typeof(SerializedObject));
            SerializedObject serialized_values = (SerializedObject)info.GetValue("values", typeof(SerializedObject));

            //bitwise & operator used to make sure both run
            if (instance.TryGetOrDeserializeFully(key_array_type, serialized_keys, out object keys) & instance.TryGetOrDeserializeFully(value_array_type, serialized_values, out object values))
            {
                AddAllValues(o, keys, values, args[0], args[1]);
                return SerializationStatus.Deserialized(o);
            }

            return SerializationStatus.FixupLater(o, CreateFixup(args[0], args[1]), new ulong[] { serialized_keys.Id, serialized_values.Id });
        }

        private void AddAllValues(object o, object keys, object values, Type key_type, Type value_type)
        {
            Action<Dictionary<object, object>, object[], object[]> add_all_values = AddAllValues;
            add_all_values.Method.GetGenericMethodDefinition().MakeGenericMethod(key_type, value_type).Invoke(this, new object[] { o, keys, values });
        }

        private void AddAllValues<TKey, TValue>(Dictionary<TKey, TValue> dictionary, TKey[] keys, TValue[] values)
        {
            for (int i = 0; i < keys.Length; i++)
                dictionary.Add(keys[i], values[i]);
        }

        private SerializationFixup CreateFixup(Type key_type, Type value_type)
        {
            return fixup;

            SerializationStatus fixup(SerializationInstance instance, object obj, ulong[] ids, object[] values)
            {
                if (!instance.CheckFullyDeserialized(values[0]) || !instance.CheckFullyDeserialized(values[1]))
                    return SerializationStatus.Failure;

                AddAllValues(obj, values[0], values[1], key_type, value_type);

                return SerializationStatus.Success;
            }
        }

        public SerializationStatus Serialize(SerializationInstance instance, object obj, SerializationInfo info, StreamingContext context)
        {
            Type[] args = obj.GetType().GetGenericArguments();

            GetAllValues(obj, out object keys, out object values, args[0], args[1]);

            info.AddValue("keys", instance.Serialize(keys));
            info.AddValue("values", instance.Serialize(values));

            return SerializationStatus.Success;
        }

        private delegate void GetAllValuesDelegate<TKey, TValue>(Dictionary<TKey, TValue> dictionary, out TKey[] keys, out TValue[] values);

        private void GetAllValues(object o, out object keys, out object values, Type key_type, Type value_type)
        {
            object[] parameters = new object[] { o, null, null };
            GetAllValuesDelegate<object, object> get_all_values = GetAllValues;
            get_all_values.Method.GetGenericMethodDefinition().MakeGenericMethod(key_type, value_type).Invoke(this, parameters);
            keys = parameters[1];
            values = parameters[2];
        }

        private void GetAllValues<TKey, TValue>(Dictionary<TKey, TValue> dictionary, out TKey[] keys, out TValue[] values)
        {
            List<TKey> k = new List<TKey>();
            List<TValue> v = new List<TValue>();
            foreach (KeyValuePair<TKey, TValue> kvp in dictionary)
            {
                k.Add(kvp.Key);
                v.Add(kvp.Value);
            }

            keys = k.ToArray();
            values = v.ToArray();
        }
    }
}

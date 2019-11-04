using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

namespace MimiTools.Serialization
{
    [Serializable]
    public struct SerializedObject : ISerializable
    {
        public static SerializedObject CreateReferenceObject(SerializedObject obj)
            => new SerializedObject(obj.Context, obj.Id);

        private static ushort GetFreeIndex(SerializationInfo info)
        {
            HashSet<ushort> indexes = new HashSet<ushort>();
            foreach (SerializationEntry entry in info)
            {
                Match m = IDRegex.Match(entry.Name);
                if (!m.Success)
                    continue;

                if (ushort.TryParse(m.Groups[IndexGroup].Value, out ushort result))
                    indexes.Add(result);
            }

            for (ushort index = 0; index <= ushort.MaxValue; index++)
            {
                if (!indexes.Contains(index))
                    return index;
            }

            throw new InvalidOperationException();
        }

        private static void GetId(SerializationInfo info, out ulong id, out ushort index, out bool reference)
        {
            HashSet<ushort> indexes = new HashSet<ushort>();
            HashSet<ushort> ref_values = new HashSet<ushort>();

            foreach (SerializationEntry entry in info)
            {
                Match m = IDRegex.Match(entry.Name);
                if (!m.Success)
                    continue;

                if (ushort.TryParse(m.Groups[IndexGroup].Value, out ushort result))
                {
                    if (m.Groups[RefGroup].Success)
                        ref_values.Add(result);
                    else
                        indexes.Add(result);
                }
            }

            id = 1;
            reference = false;

            for (index = 0; index <= ushort.MaxValue; index++)
            {
                if (!indexes.Contains(index))
                    break;

                if (index == ushort.MaxValue)
                    throw new InvalidOperationException();
            }

            for (; index >= 0; index--)
                try
                {
                    if (!ref_values.Contains(index))
                    {
                        if (index == 0)
                            break;
                        continue;
                    }
                    id = info.GetUInt64(IDPrefix + index);
                    reference = info.GetBoolean(IDPrefix + index + RefSuffix);
                }
                catch (InvalidCastException)
                {
                    id = 1;
                    if (index == 0)
                        break; // Don't run third addition
                }
        }

        private const string IDPrefix = "$id";
        private const string RefSuffix = "-ref";

        private const string IndexGroup = "index";
        private const string RefGroup = "ref";

        private static readonly Regex IDRegex = new Regex($@"^{Regex.Escape(IDPrefix)}(?<{IndexGroup}>\d+)(?<{RefGroup}>{RefSuffix})?$",
            RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.ExplicitCapture);

        public static SerializedObject Null = new SerializedObject();

        internal StreamingContext Context { get; }
        internal SerializationInfo Info { get; }

        public ulong Id { get; }
        public ushort Id_Index { get; }
        public bool IsReference { get; }

        public bool IsNull => Id == 0;

        public Type SerializedType { get => Info?.ObjectType; }

        private SerializedObject(SerializationInfo info, StreamingContext context)
        {
            Info = info ?? throw new ArgumentNullException(nameof(info));
            Context = context;

            GetId(info, out ulong id, out ushort index, out bool reference);
            Id = id;
            Id_Index = index;
            IsReference = reference;
        }

        //Serialization Constructor, internal to specifying IDs of the objects in question
        internal SerializedObject(SerializationInfo info, StreamingContext context, ulong id)
        {
            Info = info;
            Context = context;
            Id = id;
            Id_Index = GetFreeIndex(info);
            IsReference = false;
        }

        //Reference Constructor, internal to allow the serialization process to make reference types without needing another SerializationObject
        internal SerializedObject(StreamingContext context, ulong id)
        {
            Id = id;
            Id_Index = 0;
            Context = context;
            Info = null;
            IsReference = true;
        }

        public object Deserialize(Type t)
            => SerializationLibrary.Standard.Deserialize(t, this);

        public object Deserialize(Type t, StreamingContext context)
            => SerializationLibrary.Standard.Deserialize(t, this, context);

        public object Deserialize(SerializationLibrary lib, Type t)
            => lib.Deserialize(t, this);

        public object Deserialize(SerializationLibrary lib, Type t, StreamingContext context)
            => lib.Deserialize(t, this, context);

        public T Deserialize<T>()
            => (T)SerializationLibrary.Standard.Deserialize(typeof(T), this);

        public T Deserialize<T>(StreamingContext context)
            => (T)SerializationLibrary.Standard.Deserialize(typeof(T), this, context);

        public T Deserialize<T>(SerializationLibrary lib)
            => (T)lib.Deserialize(typeof(T), this);

        public T Deserialize<T>(SerializationLibrary lib, StreamingContext context)
            => (T)lib.Deserialize(typeof(T), this);

        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            string id_entry = $"{IDPrefix}{Id_Index}";
            if (Info != null)
            {
                bool id_written = false;

                foreach (SerializationEntry entry in Info) //Stopgap measure, it is highly recommended you deserialize and call it instead of calling this
                {
                    if (entry.Name == id_entry)
                        id_written = true;
                    info.AddValue(entry.Name, entry.Value, entry.ObjectType);
                }

                if (!id_written)
                    info.AddValue(id_entry, Id);
            }
            else
                info.AddValue(id_entry, Id);
        }

        public T GetValue<T>(string item)
            => (T)Info.GetValue(item, typeof(T));
    }
}

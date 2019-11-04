using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace MimiTools.Serialization
{
    public sealed class SerializationInstance
    {
        internal SerializationInstance(SerializationLibrary lib, StreamingContext context)
        {
            Library = lib;
            Context = context;
        }

        private readonly StreamingContext Context;
        private readonly SerializationLibrary Library;

        private ulong CurrentId = 0UL;
        private readonly Dictionary<object, ulong> SerializingObjects = new Dictionary<object, ulong>();
        private readonly Dictionary<object, SerializedObject> SerializationChart = new Dictionary<object, SerializedObject>();

        private readonly HashSet<ulong> DeserializingIDs = new HashSet<ulong>();
        private readonly HashSet<ulong> FixupIDs = new HashSet<ulong>();
        private readonly Dictionary<ulong, object> ObjectChart = new Dictionary<ulong, object>();
        private readonly Dictionary<ulong, HashSet<Fixup>> Fixes = new Dictionary<ulong, HashSet<Fixup>>();

        private void AddFixup(ulong id, Fixup fixup)
        {
            FixupIDs.Add(id);
            foreach (ulong required_ids in fixup.RequiredIDs)
            {
                if (!Fixes.TryGetValue(required_ids, out HashSet<Fixup> set))
                {
                    set = new HashSet<Fixup>();
                    Fixes[required_ids] = set;
                }
                set.Add(fixup);
            }
        }

        public bool CheckFullyDeserialized(object o)
        {
            if (!SerializationChart.TryGetValue(o, out SerializedObject source))
                return true;

            return !FixupIDs.Contains(source.Id);
        }

        public void CopyAndSerialize(SerializationInfo src, SerializationInfo dst)
        {
            foreach (SerializationEntry entry in src)
                dst.AddValue(entry.Name, Serialize(entry.Value));
        }

        //Creates a SerializationInfo that loops back to the current serialization instance, allowing this instance to take control of serialization of the inner objects.
        public SerializationInfo CreateLoopbackInfo(SerializationInfo info)
            => new ProxyFormatter(this, info).CreateProxy();

        private object Deserialize(Type t, SerializedObject obj)
        {
            if (ObjectChart.TryGetValue(obj.Id, out object value))
                return value;

            foreach (ISerializer serializer in Library.GetDeserializers(t))
            {
                SerializationStatus status = serializer.Deserialize(this, t, obj.Info, Context);
                if (status == null)
                    continue;
                switch (status.Result)
                {
                    case SerializationResult.Deserialized:
                    case SerializationResult.Success:
                        {
                            ObjectChart.Add(obj.Id, status.Object);

                            if (!status.Object?.GetType().IsPrimitive ?? false)
                                SerializationChart.Add(status.Object, obj);

                            RunFixups(obj.Id);

                            return status.Object;
                        }

                    case SerializationResult.NeedsFixing:
                        {
                            ObjectChart.Add(obj.Id, status.Object);

                            if (!status.Object?.GetType().IsPrimitive ?? false)
                                SerializationChart.Add(status.Object, obj);

                            AddFixup(obj.Id, new Fixup(this, status.Object, status.Fix, status.RequiredIDs));
                            RunFixups(obj.Id);

                            return status.Object;
                        }

                    case SerializationResult.UseAlternative:
                        return Deserialize(status.AltType, obj);
                }
            }

            throw new SerializationException("Object couldn't be deserialized!");
        }

        private void RunFixups(ulong id)
        {
            if (Fixes.TryGetValue(id, out HashSet<Fixup> fixes))
            {
                Fixes.Remove(id);
                foreach (Fixup fix in fixes)
                {
                    ulong fixed_id = SerializationChart[fix.Object].Id;
                    SerializationStatus fix_status = fix.Run();
                    switch (fix_status.Result)
                    {
                        case SerializationResult.Success:
                            FixupIDs.Remove(fixed_id);
                            RunFixups(fixed_id);
                            break;
                        case SerializationResult.Failure:
                            break;
                        default:
                            throw new SerializationException("Fix didn't return a valid status!");
                    }
                }
            }
        }

        public SerializedObject Serialize(object obj)
            => Serialize(obj?.GetType(), obj, true);

        private SerializedObject Serialize(Type t, object obj, bool run_checks)
        {
            ulong object_id;

            if (run_checks)
            {
                if (obj == null)
                    return SerializedObject.Null;

                if (SerializationChart.TryGetValue(obj, out SerializedObject value))
                    return value;

                if (SerializingObjects.TryGetValue(obj, out object_id))
                    return new SerializedObject(Context, object_id); // Reference Object

                object_id = ++CurrentId;
            }
            else
                object_id = SerializingObjects[obj];

            SerializingObjects.Add(obj, object_id);

            foreach (ISerializer serializer in Library.GetSerializers(t))
            {
                SerializationInfo info = new SerializationInfo(obj.GetType(), new FormatterConverter());
                SerializationStatus status = serializer.Serialize(this, obj, info, Context);

                if (status == null)
                    continue;

                switch (status.Result)
                {
                    case SerializationResult.NeedsFixing:
                        throw new SerializationException("Serializer didn't return a valid status!");
                    case SerializationResult.Failure:
                        continue;
                    case SerializationResult.Success:
                        {
                            SerializedObject result = new SerializedObject(info, Context, object_id);
                            SerializationChart.Add(object_id, result);
                            ObjectChart.Add(object_id, obj);
                            SerializingObjects.Remove(obj);
                            return result;
                        }
                    case SerializationResult.UseAlternative:
                        return Serialize(status.AltType, obj, false);
                    default:
                        throw new SerializationException("Serializer didn't return a valid status!");
                }
            }

            throw new SerializationException("Unable to serialize object!");
        }

        internal object StartDeserialize(Type t, SerializedObject obj)
        {
            if (!TryGetOrDeserialize(t, obj, out object value))
                throw new SerializationException("Was unable to deserialize object!");

            //Forcibly finish all fixups. Ideally we'd wait until all the objects fully deserialize, but loops could easily cause this to fail,
            //so if push comes to shove and we've done initial processing on all the objects, then we just have to force the fixups to run!
            //Hopefully there aren't any poorly written ones...
            int remaining = FixupIDs.Count;
            while (remaining > 0)
            {
                foreach (ulong id in FixupIDs.ToList().Where(f => FixupIDs.Contains(f)))
                    RunFixups(id);
                if (FixupIDs.Count == remaining)
                    throw new SerializationException("Unable to finish deserializing all the objects! (maybe there's a bad fixup? or maybe there's an unfilled reference!)");
                remaining = FixupIDs.Count;
            }

            return value;
        }

        public bool TryGetOrDeserialize(Type t, SerializedObject obj, out object value)
        {
            value = null;

            if (obj.Id == 0)
                return true;

            if (!DeserializingIDs.Add(obj.Id))
            {
                if (obj.Id == 1 && !obj.IsReference)
                    return new SerializationInstance(Library, Context).TryGetOrDeserialize(t, obj, out value);

                return false;
            }

            if (obj.IsReference && !ObjectChart.TryGetValue(obj.Id, out value))
                return false;

            value = Deserialize(t, obj);

            DeserializingIDs.Remove(obj.Id);

            return true;
        }

        public bool TryGetOrDeserializeFully(Type t, SerializedObject obj, out object value)
            => TryGetOrDeserializeFully(t, obj, out value) && CheckFullyDeserialized(value);

        private class Fixup
        {
            internal Fixup(SerializationInstance instance, object o, SerializationFixup fix, ulong[] required_ids)
            {
                Completed = false;
                Fix = fix;
                Instance = instance;
                Object = o;
                RequiredIDs = new ulong[required_ids.Length];
                required_ids.CopyTo(RequiredIDs, 0);
            }

            private readonly bool Completed;
            private readonly SerializationFixup Fix;
            private readonly SerializationInstance Instance;
            internal readonly object Object;
            internal readonly ulong[] RequiredIDs;

            internal SerializationStatus Run()
            {
                if (Completed)
                    return SerializationStatus.Failure;

                object[] values = new object[RequiredIDs.Length];
                for (int i = 0; i < RequiredIDs.Length; i++)
                {
                    ulong id = RequiredIDs[i];
                    if (!Instance.ObjectChart.TryGetValue(id, out object value))
                        return SerializationStatus.Failure;

                    values[i] = value;
                }

                return Fix(Instance, Object, RequiredIDs, values);
            }
        }

        private class ProxyFormatter : IFormatterConverter
        {
            private readonly SerializationInstance Instance;
            private readonly SerializationInfo Info;

            internal ProxyFormatter(SerializationInstance instance, SerializationInfo info)
            {
                Instance = instance;
                Info = info;
            }

            public SerializationInfo CreateProxy()
            {
                SerializationInfo proxy = new SerializationInfo(Info.ObjectType, this);
                foreach (SerializationEntry entry in Info)
                    proxy.AddValue(entry.Name, entry.Name);
                return proxy;
            }

            public object Convert(object value, Type type)
            {
                string name = (string)value;
                SerializedObject obj = (SerializedObject)Info.GetValue(name, typeof(SerializedObject));
                return Instance.Deserialize(type, obj);
            }

            object IFormatterConverter.Convert(object value, TypeCode typeCode)
            {
                Type t = Type.GetType($"System.{typeCode}", true, false);
                return Convert(value, t);
            }

            bool IFormatterConverter.ToBoolean(object value)
                => (bool)Convert(value, typeof(bool));

            byte IFormatterConverter.ToByte(object value)
                => (byte)Convert(value, typeof(byte));

            char IFormatterConverter.ToChar(object value)
                => (char)Convert(value, typeof(char));

            DateTime IFormatterConverter.ToDateTime(object value)
                => (DateTime)Convert(value, typeof(DateTime));

            decimal IFormatterConverter.ToDecimal(object value)
                => (decimal)Convert(value, typeof(decimal));

            double IFormatterConverter.ToDouble(object value)
                => (double)Convert(value, typeof(double));

            short IFormatterConverter.ToInt16(object value)
                => (short)Convert(value, typeof(short));

            int IFormatterConverter.ToInt32(object value)
                => (int)Convert(value, typeof(int));

            long IFormatterConverter.ToInt64(object value)
                => (long)Convert(value, typeof(long));

            sbyte IFormatterConverter.ToSByte(object value)
                => (sbyte)Convert(value, typeof(sbyte));

            float IFormatterConverter.ToSingle(object value)
                => (float)Convert(value, typeof(float));

            string IFormatterConverter.ToString(object value)
                => (string)Convert(value, typeof(string));

            ushort IFormatterConverter.ToUInt16(object value)
                => (ushort)Convert(value, typeof(ushort));

            uint IFormatterConverter.ToUInt32(object value)
                => (uint)Convert(value, typeof(uint));

            ulong IFormatterConverter.ToUInt64(object value)
                => (ulong)Convert(value, typeof(ulong));
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace MimiTools.Serialization.Serializers
{
    public class ShallowDeserializer : ISerializer
    {
        public IEnumerable<Type> DeserializableTypes => null; // Setting both to null makes it a default serializer

        public IEnumerable<Type> SerializableTypes => null; // Setting both to null makes it a default serializer

        private bool CheckGetAndSet(MemberInfo mi, bool get, bool set, bool public_only)
        {
            switch (mi.MemberType)
            {
                case (MemberTypes.Field):
                    {
                        if (!(mi is FieldInfo fi))
                            return false;

                        if (set && fi.IsInitOnly)
                            return false;

                        return !public_only || fi.IsPublic;
                    }
                case (MemberTypes.Property):
                    {
                        if (!(mi is PropertyInfo pi))
                            return false;

                        if (get && (!pi.CanRead || (public_only && pi.GetMethod.IsPublic)))
                            return false;

                        if (set && (!pi.CanWrite || (public_only && pi.SetMethod.IsPublic)))
                            return false;

                        return true;
                    }
            }
            return false;
        }

        private SerializationFixup CreateObjectFixup(MemberInfo[] missing, StreamingContext context)
        {
            return fixup;

            SerializationStatus fixup(SerializationInstance instance, object obj, ulong[] ids, object[] values)
            {
                for (int i = 0; i < missing.Length; i++)
                    SetValue(obj, missing[i], values[i]);

                InvokeOnSerialize(obj, context, false, true);

                return SerializationStatus.Success;
            }
        }

        private SerializationFixup CreateSerializableFixup(MemberInfo[] missing, StreamingContext context)
        {
            return fixup;

            SerializationStatus fixup(SerializationInstance instance, object obj, ulong[] ids, object[] values)
            {
                FormatterServices.PopulateObjectMembers(obj, missing, values);
                InvokeOnSerialize(obj, context, false, true);
                return SerializationStatus.Success;
            }
        }

        private object DefaultValue(Type t)
            => t.IsValueType ? Activator.CreateInstance(t) : null;

        public SerializationStatus Deserialize(SerializationInstance instance, Type t, SerializationInfo info, StreamingContext context)
        {
            if (t.IsInterface || t.IsAbstract)
                return SerializationStatus.Failure;

            if (t.IsSerializable)
                return DeserializeSerializable(instance, t, info, context);

            if (t.CustomAttributes.Any(d => typeof(DataContractAttribute).IsAssignableFrom(d.AttributeType)))
                return DeserializeContract(instance, t, info, context);

            return DeserializeObject(instance, t, info, context);
        }

        private SerializationStatus DeserializeContract(SerializationInstance instance, Type t, SerializationInfo info, StreamingContext context)
        {
            HashSet<string> items = new HashSet<string>();

            foreach (SerializationEntry entry in info)
                items.Add(entry.Name);

            IEnumerable<Tuple<MemberInfo, DataMemberAttribute>> members = t.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(mi => CheckGetAndSet(mi, false, true, false)).Select(mi => Tuple.Create(mi, mi.GetCustomAttribute<DataMemberAttribute>()))
                .Where(tup => tup.Item2 != null).OrderBy(tup => tup.Item2.Order);

            object obj = FormatterServices.GetUninitializedObject(t);

            foreach (Tuple<MemberInfo, DataMemberAttribute> tup in members)
            {
                MemberInfo mi = tup.Item1;
                DataMemberAttribute dma = tup.Item2;

                string name = dma.IsNameSetExplicitly ? dma.Name : mi.Name;
                if (dma.IsRequired && !items.Contains(name))
                    return SerializationStatus.Failure;

                object value = info.GetValue(name, GetMemberType(mi));

                SetValue(obj, mi, value);
            }

            InvokeOnSerialize(obj, context, false, true);
            return SerializationStatus.Deserialized(obj);
        }

        private SerializationStatus DeserializeObject(SerializationInstance instance, Type t, SerializationInfo s_info, StreamingContext context)
        {
            object obj = t.GetConstructor(Type.EmptyTypes)?.Invoke(Array.Empty<object>());

            if (obj == null)
                return SerializationStatus.Failure;

            Dictionary<string, MemberInfo> map = t.GetMembers().Where(mi => CheckGetAndSet(mi, false, true, true)).ToDictionary(mi => mi.Name);

            foreach (SerializationEntry entry in s_info)
            {
                if (!map.TryGetValue(entry.Name, out MemberInfo mi))
                    continue;

                object value = s_info.GetValue(entry.Name, GetMemberType(mi));

                SetValue(obj, mi, value);
            }

            return SerializationStatus.Deserialized(obj);
        }

        private SerializationStatus DeserializeSerializable(SerializationInstance instance, Type t, SerializationInfo info, StreamingContext context)
        {
            object obj = FormatterServices.GetUninitializedObject(t);
            InvokeOnSerialize(obj, context, false, false);

            if (typeof(ISerializable).IsAssignableFrom(t) && TryDeserializeISerializable(obj, info, context))
            {
                InvokeOnSerialize(obj, context, false, true);
                return SerializationStatus.Deserialized(obj);
            }

            Dictionary<string, MemberInfo> map = FormatterServices.GetSerializableMembers(t, context).ToDictionary(m => m.Name);

            List<MemberInfo> found = new List<MemberInfo>();
            List<object> values = new List<object>();

            foreach (SerializationEntry entry in info)
            {
                if (!map.TryGetValue(entry.Name, out MemberInfo mi))
                    continue;

                found.Add(mi);
                values.Add(info.GetValue(entry.Name, GetMemberType(mi)));
            }

            obj = FormatterServices.PopulateObjectMembers(obj, found.ToArray(), values.ToArray());

            InvokeOnSerialize(obj, context, false, true);
            return SerializationStatus.Deserialized(obj);
        }

        private Type GetMemberType(MemberInfo info)
        {
            switch (info.MemberType)
            {
                case MemberTypes.Field:
                    return (info as FieldInfo)?.FieldType;
                case MemberTypes.Property:
                    return (info as PropertyInfo)?.PropertyType;
            }
            return null;
        }

        private object GetValue(object obj, MemberInfo info)
        {
            switch (info.MemberType)
            {
                case MemberTypes.Field:
                    return (info as FieldInfo)?.GetValue(obj);
                case MemberTypes.Property:
                    return (info as PropertyInfo)?.GetValue(obj);
            }
            return null;
        }

        private void InvokeOnSerialize(object o, StreamingContext context, bool serializing, bool finished)
        {
            Type attr;
            if (serializing)
                attr = finished ? typeof(OnSerializedAttribute) : typeof(OnSerializingAttribute);
            else
                attr = finished ? typeof(OnDeserializedAttribute) : typeof(OnDeserializingAttribute);

            HashSet<MethodInfo> run_methods = new HashSet<MethodInfo>();

            Type t = o.GetType();

            while (t != null)
            {
                Queue<MethodInfo> methods = new Queue<MethodInfo>(o.GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic).Where(Filter));
                while (methods.Count > 0)
                {
                    MethodInfo mi = methods.Dequeue();

                    if (!run_methods.Add(mi.GetBaseDefinition()))
                        continue;

                    if (!(mi.CreateDelegate(typeof(Action<StreamingContext>), o) is Action<StreamingContext> a))
                        continue;

                    a(context);
                }

                t = t.BaseType;
            }

            bool Filter(MethodInfo mi)
            {
                ParameterInfo[] p_info_array = mi.GetParameters();
                if (p_info_array.Length != 1)
                    return false;
                ParameterInfo info = p_info_array[0];

                if (info.ParameterType != typeof(StreamingContext))
                    return false;

                if (mi.ReturnType != typeof(void))
                    return false;

                if (run_methods.Contains(mi.GetBaseDefinition()))
                    return false;

                return true;
            }
        }

        public SerializationStatus Serialize(SerializationInstance instance, object obj, SerializationInfo info, StreamingContext context)
        {
            Type t = obj.GetType();

            if (t.IsSerializable)
                return SerializeSerializable(instance, obj, info, context);

            if (t.CustomAttributes.Any(d => typeof(DataContractAttribute).IsAssignableFrom(d.AttributeType)))
                return SerializeContract(instance, obj, info, context);

            return SerializeObject(instance, obj, info, context);
        }

        private SerializationStatus SerializeContract(SerializationInstance instance, object obj, SerializationInfo info, StreamingContext context)
        {
            Type t = obj.GetType();

            IEnumerable<Tuple<MemberInfo, DataMemberAttribute>> members = t.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Select(mi => Tuple.Create(mi, mi.GetCustomAttribute<DataMemberAttribute>()))
                .Where(tup => tup.Item2 != null).OrderBy(tup => tup.Item2.Order);

            InvokeOnSerialize(obj, context, true, false);

            foreach (Tuple<MemberInfo, DataMemberAttribute> tuple in members)
            {
                MemberInfo mi = tuple.Item1;
                DataMemberAttribute attr = tuple.Item2;

                string name = attr.IsNameSetExplicitly ? attr.Name : mi.Name;

                object value = GetValue(obj, mi);

                if (!attr.IsRequired && !attr.EmitDefaultValue && value == DefaultValue(GetMemberType(mi)))
                    continue;

                info.AddValue(name, value);
            }

            InvokeOnSerialize(obj, context, true, true);

            return SerializationStatus.Success;
        }

        private SerializationStatus SerializeObject(SerializationInstance instance, object obj, SerializationInfo info, StreamingContext context)
        {
            foreach (MemberInfo mi in obj.GetType().GetMembers().Where(mi => CheckGetAndSet(mi, true, false, true)))
                info.AddValue(mi.Name, GetValue(obj, mi));

            return SerializationStatus.Success;
        }

        public SerializationStatus SerializeSerializable(SerializationInstance instance, object obj, SerializationInfo info, StreamingContext context)
        {
            InvokeOnSerialize(obj, context, true, false);
            if (obj is ISerializable serializable)
            {
                serializable.GetObjectData(info, context);
                InvokeOnSerialize(obj, context, true, true);
                return SerializationStatus.Success;
            }

            MemberInfo[] infos = FormatterServices.GetSerializableMembers(obj.GetType(), context);
            object[] objects = FormatterServices.GetObjectData(obj, infos);

            for (int i = 0; i < infos.Length; i++)
                info.AddValue(infos[i].Name, objects[i]);

            InvokeOnSerialize(obj, context, true, true);

            return SerializationStatus.Success;
        }

        private void SetValue(object obj, MemberInfo info, object value)
        {
            switch (info.MemberType)
            {
                case MemberTypes.Field:
                    (info as FieldInfo)?.SetValue(obj, value);
                    break;
                case MemberTypes.Property:
                    (info as PropertyInfo)?.SetValue(obj, value);
                    break;
            }
        }

        private bool TryDeserializeISerializable(object o, SerializationInfo serializationInfo, StreamingContext context)
        {
            ConstructorInfo info =
                o.GetType().GetConstructor(
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly,
                    null, new Type[] { typeof(SerializationInfo), typeof(StreamingContext) }, null);

            if (info == null)
                return false;

            info.Invoke(o, new object[] { info, context });

            return true;
        }
    }
}

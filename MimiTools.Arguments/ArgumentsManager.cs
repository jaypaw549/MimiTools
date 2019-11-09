using MimiTools.Arguments.Converters;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace MimiTools.Arguments
{
    public class ArgumentsManager
    {
        private static readonly Comparer<(TargetData, FlagArgumentAttribute)> _flag_comparer =
            Comparer<(TargetData, FlagArgumentAttribute)>.Create(ComparerMethod);

        private static readonly Comparer<(TargetData, PositionArgumentAttribute)> _pos_comparer =
            Comparer<(TargetData, PositionArgumentAttribute)>.Create(ComparerMethod);

        public ArgumentsManager(Type t)
        {
            _ctor = CreateConstructorDelegate(t);
            _converter_list = new HashSet<IArgumentConverter>();
            _converters = new Dictionary<Type, HashSet<IArgumentConverter>>();
            _flag_args = GetFlagData(t);
            _pos_args = GetPositionData(t);
        }

        private ArgumentsManager(ArgumentsManager origin, Type t)
        {
            _ctor = origin._ctor;
            _converter_list = origin._converter_list;
            _converters = origin._converters;
            _flag_args = GetFlagData(t);
            _pos_args = GetPositionData(t);
        }

        internal const AttributeTargets _usage = AttributeTargets.Field | AttributeTargets.Method | AttributeTargets.Property;
        private const BindingFlags _flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        private readonly Func<object> _ctor;
        private readonly HashSet<IArgumentConverter> _converter_list;
        private readonly Dictionary<Type, HashSet<IArgumentConverter>> _converters;
        private readonly Dictionary<string, TargetData[]> _flag_args;
        private readonly PositionData[] _pos_args;

        public bool AddConverter(IArgumentConverter converter)
        {
            if (!_converter_list.Add(converter ?? throw new ArgumentNullException(nameof(converter))))
                return false;

            foreach (KeyValuePair<Type, HashSet<IArgumentConverter>> kvp in _converters)
                if (converter.GetCompatibilty(kvp.Key) == ConversionCompability.Possible)
                    kvp.Value.Add(converter);

            return true;
        }

        public object Parse(string args)
            => Parse(new StringArguments(args ?? throw new ArgumentNullException(nameof(args))));

        public object Parse(IEnumerable<string> args)
        {
            if (args == null)
                throw new ArgumentNullException(nameof(args));
            
            return Parse(args.GetEnumerator(), false);
        }

        private object Parse(IEnumerator<string> args, bool allow_overflow)
        {
            object value = _ctor();
            using SaveState data = new SaveState(args);
            int pos = 0;

            while (true)
            {
                data.Save();
                if (pos == _pos_args.Length || _pos_args[pos].AllowFlags)
                {

                    if (data.MoveNext()
                        && _flag_args.TryGetValue(data.Current, out TargetData[] targets)
                        && TryExecAnyTarget(targets, value, data))
                        continue;

                    data.Reset();
                }

                if (pos < _pos_args.Length && TryExecAnyTarget(_pos_args[pos].Targets, value, data))
                {
                    pos++;
                    continue;
                }

                break;
            }

            return value;
        }

        public bool TryConvert(string data, Type type, out object obj)
        {
            if (!_converters.TryGetValue(type, out HashSet<IArgumentConverter> converters))
            {
                converters = new HashSet<IArgumentConverter>();
                foreach (IArgumentConverter arg_converter in _converter_list)
                    if (arg_converter.GetCompatibilty(type) == ConversionCompability.Possible)
                        converters.Add(arg_converter);
                _converters[type] = converters;
            }

            foreach (IArgumentConverter c in converters)
                if (c.TryConvert(data, type, out obj))
                    return true;

            foreach (IArgumentConverter c in _converter_list)
                if (c.TryConvert(data, type, out obj))
                {
                    _converters[type].Add(c);
                    return true;
                }

            obj = null;
            return false;
        }

        private bool TryExecAnyTarget(TargetData[] targets, object target, SaveState data)
        {
            for (int i = 0; i < targets.Length; i++)
                if (TryExecTarget(targets[i], target, data))
                    return true;

            return false;
        }

        private bool TryExecTarget(in TargetData targetData, object target, SaveState data)
        {
            ParamData[] p_data = targetData.Arguments;
            object[] parameters = new object[targetData.Arguments.Length];
            for(int i = 0; i < p_data.Length; i++)
            {
                if (p_data[i].Type == null)
                {
                    parameters[i] = null;
                    continue;
                }

                if (!data.MoveNext() || !TryConvert(data.Current, p_data[i].Type, out object obj))
                {
                    data.Reset();
                    return false;
                }

                parameters[i] = obj;
            }

            targetData.Set(target, parameters);
            return true;
        }

        public static ArgumentsManager Create(Type t)
        {
            ArgumentsManager arg_mgr = new ArgumentsManager(t);
            arg_mgr.AddConverter(BasicConverter.Instance);
            arg_mgr.AddConverter(EnumConverter.Instance);
            arg_mgr.AddConverter(new NullableConverter(arg_mgr));
            return arg_mgr;
        }

        private static Func<object> CreateConstructorDelegate(Type t)
        {
            ConstructorInfo ctor = t.GetConstructor(
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                    null, Type.EmptyTypes, null);

            if (ctor == null)
                throw new ArgumentException("Type must have a no parameter's constructor!");

            DynamicMethod constructor = new DynamicMethod("Construct", typeof(object), Type.EmptyTypes, t);
            ILGenerator il = constructor.GetILGenerator();
            il.Emit(OpCodes.Newobj, ctor);

            if (t.IsValueType)
                il.Emit(OpCodes.Box, t);

            il.Emit(OpCodes.Ret);
            return (Func<object>)constructor.CreateDelegate(typeof(Func<object>));
        }

        private static Dictionary<string, TargetData[]> GetFlagData(Type t)
        {
            var dict = new Dictionary<string, List<(TargetData, FlagArgumentAttribute)>>();
            foreach (MemberInfo mi in t.GetMembers(_flags))
            {
                TargetData? data = null;
                foreach(FlagArgumentAttribute attr in mi.GetCustomAttributes<FlagArgumentAttribute>())
                {
                    if (data == null)
                        data = GetTargetData(mi);
                    if (!dict.TryGetValue(attr.Flag, out var list))
                        dict[attr.Flag] = list = new List<(TargetData, FlagArgumentAttribute)>();
                    list.Add((data.Value, attr));
                }
            }

            Dictionary<string, TargetData[]> flag_args = new Dictionary<string, TargetData[]>();
            foreach(var kvp in dict)
            {
                kvp.Value.Sort(_flag_comparer);
                TargetData[] data = new TargetData[kvp.Value.Count];
                for (int i = 0; i < data.Length; i++)
                    data[i] = kvp.Value[i].Item1;
                flag_args.Add(kvp.Key, data);
            }

            return flag_args;
        }

        private static PositionData[] GetPositionData(Type t)
        {
            var list = new List<(TargetData, PositionArgumentAttribute)>();
            foreach (MemberInfo mi in t.GetMembers(_flags))
            {
                PositionArgumentAttribute attr = mi.GetCustomAttribute<PositionArgumentAttribute>();
                if (attr == null)
                    continue;

                list.Add((GetTargetData(mi), attr));
            }

            list.Sort(_pos_comparer);

            int position = list[0].Item2.Position;
            bool flags = true;
            List<PositionData> p_data = new List<PositionData>();
            List<TargetData> t_data = new List<TargetData>();
            foreach(var tuple in list)
            {
                if (tuple.Item2.Position != position)
                {
                    p_data.Add(new PositionData(t_data.ToArray(), flags));
                    flags = true;
                    t_data.Clear();
                }

                flags &= tuple.Item2.AllowFlags;
                t_data.Add(tuple.Item1);
            }

            if (t_data.Count > 0)
                p_data.Add(new PositionData(t_data.ToArray(), flags));

            return p_data.ToArray();
        }

        private static TargetData GetTargetData(MemberInfo mi)
        {
            if (mi is PropertyInfo p_info)
                return GetMethodData(p_info.SetMethod);
            if (mi is MethodInfo m_info)
                return GetMethodData(m_info);

            return GetFieldData((FieldInfo)mi);
        }

        private static TargetData GetFieldData(FieldInfo fi)
        {
            DynamicMethod set_field = new DynamicMethod($"Set{fi.Name}", typeof(void), new Type[] { typeof(object), typeof(object[]) }, fi.DeclaringType);
            ILGenerator il = set_field.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Ldelem, typeof(object));
            if (fi.FieldType.IsValueType)
            {
                il.Emit(OpCodes.Unbox, fi.FieldType);
                il.Emit(OpCodes.Ldobj, fi.FieldType);
            }
            else
                il.Emit(OpCodes.Castclass, fi.FieldType);

            il.Emit(OpCodes.Stfld, fi);
            il.Emit(OpCodes.Ret);

            return new TargetData(
                new ParamData[] { new ParamData(fi.FieldType) }, 
                (Action<object, object[]>)set_field.CreateDelegate(typeof(Action<object, object[]>)));
        }

        private static TargetData GetMethodData(MethodInfo mi)
        {
            DynamicMethod setter = new DynamicMethod($"Invoke{mi.Name}", typeof(void), new Type[] { typeof(object), typeof(object[]) }, mi.DeclaringType);
            ILGenerator il = setter.GetILGenerator();
            ParameterInfo[] parameters = mi.GetParameters();
            ParamData[] p_data = new ParamData[parameters.Length];

            il.Emit(OpCodes.Ldarg_0);
            if (mi.DeclaringType.IsValueType)
                il.Emit(OpCodes.Unbox, mi.DeclaringType);
            else
                il.Emit(OpCodes.Castclass, mi.DeclaringType);

            for(int i = 0; i < parameters.Length; i++)
            {
                Type t = parameters[i].ParameterType;

                bool by_ref = t.IsByRef;
                if (by_ref)
                    t = t.GetElementType();

                if (parameters[i].IsOut)
                {
                    p_data[i] = new ParamData(null);
                    il.Emit(OpCodes.Ldloca_S, il.DeclareLocal(t));
                    continue;
                }

                p_data[i] = new ParamData(t);

                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldc_I4, i);
                il.Emit(OpCodes.Ldelem, typeof(object));

                if (t.IsValueType)
                {
                    il.Emit(OpCodes.Unbox, t);
                    il.Emit(OpCodes.Ldobj, t);
                }
                else
                    il.Emit(OpCodes.Castclass, t);

                if (by_ref)
                {
                    LocalBuilder var = il.DeclareLocal(t);
                    il.Emit(OpCodes.Stloc_S, var);
                    il.Emit(OpCodes.Ldloca_S, var);
                }
            }

            il.Emit(OpCodes.Call, mi);
            if (mi.ReturnType != typeof(void))
                il.Emit(OpCodes.Pop);

            il.Emit(OpCodes.Ret);

            return new TargetData(p_data, (Action<object, object[]>)setter.CreateDelegate(typeof(Action<object, object[]>)));
        }

        private static int ComparerMethod((TargetData, PositionArgumentAttribute) a, (TargetData, PositionArgumentAttribute) b)
        {
            if (a.Item2.Position != b.Item2.Position)
                return a.Item2.Position.CompareTo(b.Item2.Position);
            return a.Item2.Priority.CompareTo(b.Item2.Priority);
        }

        private static int ComparerMethod((TargetData, FlagArgumentAttribute) a, (TargetData, FlagArgumentAttribute) b)
            => a.Item2.Priority.CompareTo(b.Item2.Priority);

        private readonly struct ParamData
        {
            internal readonly Type Type;

            internal ParamData(Type t)
            {
                Type = t;
            }
        }

        private readonly struct PositionData
        {
            internal PositionData(TargetData[] targets, bool flags)
            {
                AllowFlags = flags;
                Targets = targets;
            }

            internal readonly bool AllowFlags;
            internal readonly TargetData[] Targets;
        }

        private class SaveState : IEnumerator<string>
        {
            internal SaveState(IEnumerator<string> enumerator)
            {
                Current = null;
                _enumerator = enumerator;
                _save_data = new Queue<string>();
                _repeat = 0;
                _save_data.Enqueue(null);
            }

            private readonly IEnumerator<string> _enumerator;
            private readonly Queue<string> _save_data;
            private int _repeat;

            public string Current { get; private set; }

            object IEnumerator.Current => Current;

            public void Dispose()
            {
                _enumerator.Dispose();
                _save_data.Clear();
                _repeat = -1;
            }

            public bool MoveNext()
            {
                if (_repeat == -1)
                    throw new InvalidOperationException("Disposed!");

                if (_repeat > 0)
                {
                    _save_data.Enqueue(Current = _save_data.Dequeue());
                    _repeat--;
                }

                else if (_enumerator.MoveNext())
                    _save_data.Enqueue(Current = _enumerator.Current);

                else
                    return false;

                return true;
            }

            public void Reset()
            {
                if (_repeat == -1)
                    throw new InvalidOperationException("Disposed!");

                //Cycle all unfinished repeats
                while (_repeat-- > 0)
                    _save_data.Enqueue(_save_data.Dequeue());

                //Get the value from when we last saved
                _save_data.Enqueue(Current = _save_data.Dequeue());

                _repeat = _save_data.Count-1;
            }

            public void Save()
            {
                string[] data = new string[_repeat];

                for (int i = 0; i < data.Length; i++)
                    data[i] = _save_data.Dequeue();

                _save_data.Clear();

                for (int i = 0; i < data.Length; i++)
                    _save_data.Enqueue(data[i]);

                _save_data.Enqueue(Current);
            }
        }

        private readonly struct TargetData
        {
            internal TargetData(ParamData[] args, Action<object, object[]> setter)
            {
                Arguments = args;
                Set = setter;
            }
            internal readonly ParamData[] Arguments;
            internal readonly Action<object, object[]> Set;
        }
    }
}

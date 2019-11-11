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
    public class ArgumentsParser
    {

        private static readonly Comparer<(TargetData, FlagArgumentAttribute)> _flag_comparer =
            Comparer<(TargetData, FlagArgumentAttribute)>.Create(ComparerMethod);

        private static readonly Comparer<(TargetData, PositionArgumentAttribute)> _pos_comparer =
            Comparer<(TargetData, PositionArgumentAttribute)>.Create(ComparerMethod);

        public ArgumentsParser(Type t)
        {
            _ctor = CreateConstructorDelegate(t, true);
            _converter_list = new HashSet<IArgumentConverter>();
            _converters = new Dictionary<Type, HashSet<IArgumentConverter>>();
            _flag_args = GetFlagData(t);
            _parsers = new Dictionary<Type, ArgumentsParser>()
            {
                { t, this }
            };
            _pos_args = GetPositionData(t);
        }

        public ArgumentsParser(Type t, Func<object> factory)
        {
            _ctor = factory;
            _converter_list = new HashSet<IArgumentConverter>();
            _converters = new Dictionary<Type, HashSet<IArgumentConverter>>();
            _flag_args = GetFlagData(t);
            _parsers = new Dictionary<Type, ArgumentsParser>()
            {
                { t, this }
            };
            _pos_args = GetPositionData(t);
        }

        public ArgumentsParser(Type t, params IArgumentConverter[] converters)
        {
            _ctor = CreateConstructorDelegate(t, true);
            _converter_list = new HashSet<IArgumentConverter>();

            for (int i = 0; i < converters.Length; i++)
                _converter_list.Add(converters[i]);

            _converters = new Dictionary<Type, HashSet<IArgumentConverter>>();
            _flag_args = GetFlagData(t);
            _parsers = new Dictionary<Type, ArgumentsParser>()
            {
                { t, this }
            };
            _pos_args = GetPositionData(t);
        }

        public ArgumentsParser(Type t, Func<object> factory, params IArgumentConverter[] converters)
        {
            _ctor = factory;
            _converter_list = new HashSet<IArgumentConverter>();

            for (int i = 0; i < converters.Length; i++)
                _converter_list.Add(converters[i]);

            _converters = new Dictionary<Type, HashSet<IArgumentConverter>>();
            _flag_args = GetFlagData(t);
            _parsers = new Dictionary<Type, ArgumentsParser>()
            {
                { t, this }
            };
            _pos_args = GetPositionData(t);
        }

        private ArgumentsParser(ArgumentsParser origin, Type t)
        {
            _ctor = CreateConstructorDelegate(t, false);
            _converter_list = origin._converter_list;
            _converters = origin._converters;
            _flag_args = GetFlagData(t);
            _parsers = origin._parsers;
            _parsers.Add(t, this);
            _pos_args = GetPositionData(t);
        }

        internal const AttributeTargets _usage = AttributeTargets.Field | AttributeTargets.Method | AttributeTargets.Property;
        private const BindingFlags _flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        private delegate object SetFunc(object instance, object[] args);

        private readonly Func<object> _ctor;
        private readonly HashSet<IArgumentConverter> _converter_list;
        private readonly Dictionary<Type, HashSet<IArgumentConverter>> _converters;
        private readonly Dictionary<string, TargetData[]> _flag_args;
        private readonly Dictionary<Type, ArgumentsParser> _parsers;
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

        public void AddSubParser(Type t, ArgumentsParser parser)
            => _parsers.Add(t, parser);

        public ArgumentsParser GetParser(Type t)
        {
            if (!_parsers.TryGetValue(t, out ArgumentsParser parser))
                parser = _parsers[t] = new ArgumentsParser(this, t);
            return parser;
        }

        public object Parse(string args)
            => Parse(new StringArguments(args ?? throw new ArgumentNullException(nameof(args))));

        public object Parse(IEnumerable<string> args)
        {
            if (args == null)
                throw new ArgumentNullException(nameof(args));
            
            return Parse(this, args.GetEnumerator(), false);
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

        private static int ComparerMethod((TargetData, PositionArgumentAttribute) a, (TargetData, PositionArgumentAttribute) b)
        {
            if (a.Item2.Position != b.Item2.Position)
                return a.Item2.Position.CompareTo(b.Item2.Position);
            return a.Item2.Priority.CompareTo(b.Item2.Priority);
        }

        private static int ComparerMethod((TargetData, FlagArgumentAttribute) a, (TargetData, FlagArgumentAttribute) b)
            => a.Item2.Priority.CompareTo(b.Item2.Priority);

        public static ArgumentsParser Create(Type t)
        {
            ArgumentsParser arg_mgr = new ArgumentsParser(t,
                BasicConverter.Instance, EnumConverter.Instance);
            arg_mgr.AddConverter(new NullableConverter(arg_mgr));
            return arg_mgr;
        }

        private static Func<object> CreateConstructorDelegate(Type t, bool required)
        {
            ConstructorInfo ctor = t.GetConstructor(
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                    null, Type.EmptyTypes, null);


            if (t.IsValueType)
                return CreateDefaultDelegate(t);

            if (ctor == null)
            {
                if (required)
                    throw new ArgumentException("Type must have a default constructor!");
                return null;
            }

            DynamicMethod constructor = new DynamicMethod("Construct", typeof(object), Type.EmptyTypes, t);
            ILGenerator il = constructor.GetILGenerator();
            il.Emit(OpCodes.Newobj, ctor);

            il.Emit(OpCodes.Ret);
            return (Func<object>)constructor.CreateDelegate(typeof(Func<object>));
        }

        private static Func<object> CreateDefaultDelegate(Type t)
        {
            DynamicMethod method = new DynamicMethod("GetDefault", typeof(object), Type.EmptyTypes, t);
            ILGenerator il = method.GetILGenerator();
            LocalBuilder var = il.DeclareLocal(t);
            il.Emit(OpCodes.Ldloca_S, var);
            il.Emit(OpCodes.Initobj, t);
            il.Emit(OpCodes.Ldloc_S, var);
            il.Emit(OpCodes.Box, t);
            il.Emit(OpCodes.Ret);

            return (Func<object>)method.CreateDelegate(typeof(Func<object>));
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

                    if (!data.HasValue)
                        break;

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

                TargetData? data = GetTargetData(mi);
                if (data.HasValue)
                    list.Add((data.Value, attr));
            }

            if (list.Count == 0)
                return new PositionData[0];

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

        private static TargetData? GetTargetData(MemberInfo mi)
        {
            if (mi is PropertyInfo p_info)
                return GetMethodData(p_info.SetMethod, p_info.GetCustomAttribute<SubParseAttribute>() != null);
            if (mi is MethodInfo m_info)
                return GetMethodData(m_info, false);

            return GetFieldData((FieldInfo)mi);
        }

        private static TargetData? GetFieldData(FieldInfo fi)
        {
            if ((fi.Attributes & FieldAttributes.InitOnly) == FieldAttributes.InitOnly)
                return null;

            DynamicMethod set_field = new DynamicMethod($"Set{fi.Name}", typeof(object), new Type[] { typeof(object), typeof(object[]) }, fi.DeclaringType);
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
            il.Emit(OpCodes.Ldnull);
            il.Emit(OpCodes.Ret);

            return new TargetData(
                new ParamData[] { new ParamData(fi.FieldType, fi.GetCustomAttribute<SubParseAttribute>() != null) }, 
                default, (SetFunc)set_field.CreateDelegate(typeof(SetFunc)));
        }

        private static TargetData? GetMethodData(MethodInfo mi, bool sub)
        {
            if (mi == null)
                return null;

            DynamicMethod setter = new DynamicMethod($"Invoke{mi.Name}", typeof(object), new Type[] { typeof(object), typeof(object[]) }, mi.DeclaringType);
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
                bool s_parse = sub || parameters[i].GetCustomAttribute<SubParseAttribute>() != null;

                bool by_ref = t.IsByRef;
                if (by_ref)
                    t = t.GetElementType();

                if (parameters[i].IsOut)
                {
                    p_data[i] = new ParamData(null, s_parse);
                    il.Emit(OpCodes.Ldloca_S, il.DeclareLocal(t));
                    continue;
                }

                p_data[i] = new ParamData(t, s_parse);

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

            if (mi.IsVirtual)
                il.Emit(OpCodes.Callvirt, mi);
            else
                il.Emit(OpCodes.Call, mi);

            if (mi.ReturnType == typeof(void))
                il.Emit(OpCodes.Ldnull);

            il.Emit(OpCodes.Ret);

            ParseAndReturnResultAttribute attr = mi.GetCustomAttribute<ParseAndReturnResultAttribute>();
            Optional<Type> prv;

            if (attr == null)
                prv = default;
            else
                prv = new Optional<Type>(attr.ParseAs);

            return new TargetData(p_data, prv, (SetFunc)setter.CreateDelegate(typeof(SetFunc)));
        }

        private static object Parse(ArgumentsParser parser, IEnumerator<string> args, bool allow_overflow)
        {
            object value = parser._ctor?.Invoke();

            if (value == null)
                throw new InvalidOperationException("Cannot create an instance of the specified object!");

            using SaveState data = new SaveState(args);
            int pos = 0;

            while (true)
            {
                bool reset;
                data.Save();

                if (pos == parser._pos_args.Length || parser._pos_args[pos].AllowFlags)
                {
                    if (data.MoveNext()
                        && parser._flag_args.TryGetValue(data.Current, out TargetData[] targets)
                        && TryExecAnyTarget(ref parser, ref value, targets, data, out reset))
                    {
                        if (reset)
                            pos = 0;
                        continue;
                    }

                    data.Reset();
                }

                if (pos < parser._pos_args.Length && TryExecAnyTarget(ref parser, ref value, parser._pos_args[pos].Targets, data, out reset))
                {
                    if (reset)
                        pos = 0;
                    else
                        pos++;
                    continue;
                }

                break;
            }

            if (!allow_overflow && pos < parser._pos_args.Length)
                throw new ArgumentException("Unknown or invalid arguments!");

            return value;
        }

        private static bool TryExecAnyTarget(ref ArgumentsParser parser, ref object instance, TargetData[] targets, SaveState data, out bool reset_pos)
        {
            for (int i = 0; i < targets.Length; i++)
                if (TryExecTarget(ref parser, ref instance, targets[i], data, out reset_pos))
                    return true;

            return reset_pos = false;
        }

        private static bool TryExecTarget(ref ArgumentsParser parser, ref object instance, in TargetData targetData, SaveState data, out bool reset_pos)
        {
            ParamData[] p_data = targetData.Arguments;
            object[] parameters = new object[targetData.Arguments.Length];
            for (int i = 0; i < p_data.Length; i++)
            {
                if (p_data[i].Type == null)
                {
                    parameters[i] = null;
                    continue;
                }

                if (p_data[i].SubParse)
                {
                    parameters[i] = Parse(parser.GetParser(p_data[i].Type), data, true);
                    continue;
                }

                if (!data.MoveNext() || !parser.TryConvert(data.Current, p_data[i].Type, out object obj))
                {
                    data.Reset();
                    return reset_pos = false;
                }

                parameters[i] = obj;
            }

            object ret = targetData.Set(instance, parameters);
            if (targetData.ParseReturn.IsSpecified)
            {
                instance = ret;
                parser = parser.GetParser(targetData.ParseReturn.Value ?? ret.GetType());
                reset_pos = true;
            }
            else
                reset_pos = false;

            return true;
        }

        private readonly struct ParamData
        {
            internal readonly bool SubParse;

            internal readonly Type Type;

            internal ParamData(Type t, bool sub)
            {
                SubParse = sub;
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
            internal TargetData(ParamData[] args, Optional<Type> parse, SetFunc setter)
            {
                Arguments = args;
                ParseReturn = parse;
                Set = setter;
            }
            internal readonly ParamData[] Arguments;
            internal readonly Optional<Type> ParseReturn;
            internal readonly SetFunc Set;
        }
    }
}

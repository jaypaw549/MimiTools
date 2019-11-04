using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace MimiTools.Loader
{
    internal class AssemblyTracker
    {
        private readonly AssemblyBuilder _asm;

        private readonly List<Assembly> _asm_refs = new List<Assembly>();

        private readonly Dictionary<string, MethodInfo> _members = new Dictionary<string, MethodInfo>();
        private readonly Dictionary<string, ModuleTracker> _modules = new Dictionary<string, ModuleTracker>();
        private readonly Dictionary<TypeRef, TypeInfo> _types = new Dictionary<TypeRef, TypeInfo>();

        public AssemblyTracker(AssemblyName name)
        {
            _asm = AssemblyBuilder.DefineDynamicAssembly(name, AssemblyBuilderAccess.RunAndCollect);
        }

        public void AddRef(Assembly asm)
            => _asm_refs.Add(asm);

        public ModuleTracker DefineModule(string name)
        {
            ModuleTracker tracker = new ModuleTracker(this, _asm.DefineDynamicModule(name));
            _modules.Add(name, tracker);
            return tracker;
        }

        public Assembly GetAsmRef(int token)
            => _asm_refs[(token & 0xFFFFFF) - 1];

        public ModuleTracker GetModule(string name)
            => _modules[name];
    }
}

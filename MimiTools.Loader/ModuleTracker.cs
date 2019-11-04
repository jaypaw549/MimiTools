using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;

namespace MimiTools.Loader
{
    internal class ModuleTracker
    {
        public AssemblyTracker Assembly { get; }

        private readonly ModuleBuilder _mod;

        public ModuleTracker(AssemblyTracker tracker, ModuleBuilder moduleBuilder)
        {
            Assembly = tracker;
            _mod = moduleBuilder;
        }
    }
}

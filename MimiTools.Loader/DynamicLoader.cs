using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Security;
using System.Security.Cryptography;
using System.Text;



namespace MimiTools.Loader
{
    public class DynamicLoader
    {
        private readonly Dictionary<AssemblyName, Assembly> _loaded = new Dictionary<AssemblyName, Assembly>(AsmNameComparer.Instance);
        private readonly Dictionary<ByteData, StrongNameKeyPair> _key_pairs = new Dictionary<ByteData, StrongNameKeyPair>();

        public event Func<AssemblyName, byte[]> AssemblyDataResolve;
        public event Func<AssemblyName, Stream> AssemblyStreamResolve;
        public event Func<AssemblyName, string, byte[]> ModuleDataResolve;
        public event Func<AssemblyName, string, Stream> ModuleStreamResolve;

        public DynamicLoader()
        {
            AppDomain.CurrentDomain.AssemblyLoad += new AssemblyLoadEventHandler((o, args) =>
            {
                _loaded[args.LoadedAssembly.GetName()] = args.LoadedAssembly;
                Console.WriteLine(args.LoadedAssembly.GetName());
            });

            foreach(Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
                _loaded[asm.GetName()] = asm;
        }

        public void AddStrongNameKeyPair(StrongNameKeyPair pair)
            => _key_pairs.Add(new ByteData(pair.PublicKey), pair);

        private void BuildReferences(MetadataReader reader, AssemblyTracker tracker)
        {
            foreach (AssemblyReferenceHandle _handle in reader.AssemblyReferences)
            {
                AssemblyName reference = reader.GetAssemblyReference(_handle).GetAssemblyName();

                if (_loaded.TryGetValue(reference, out Assembly asm))
                    tracker.AddRef(asm);
                else
                    tracker.AddRef(LoadAssembly(reference));
            }
        }

        public unsafe Assembly LoadAssembly(byte[] assembly)
        {
            fixed(byte* b = assembly)
                using (PEReader reader = new PEReader(b, assembly.Length))
                    return LoadAssembly(reader);
        }

        public Assembly LoadAssembly(Stream assembly)
        {
            using (PEReader reader = new PEReader(assembly, PEStreamOptions.PrefetchMetadata | PEStreamOptions.PrefetchEntireImage))
                return LoadAssembly(reader);
        }

        private Assembly LoadAssembly(PEReader pe, AssemblyName name = null)
        {
            MetadataReader reader = pe.GetMetadataReader();

            if (!reader.IsAssembly)
                throw new InvalidDataException("Specified assembly must be an assembly!");

            if (name != null && !AsmNameComparer.Equals(name, reader.GetAssemblyDefinition().GetAssemblyName()))
                throw new ArgumentException("Resolved assembly doesn't match the embedded assembly name!");

            name = reader.GetAssemblyDefinition().GetAssemblyName();

            if (_key_pairs.TryGetValue(new ByteData(name.GetPublicKey()), out StrongNameKeyPair kp))
                name.KeyPair = kp;

            name.SetPublicKey(null);

            AssemblyTracker tracker = new AssemblyTracker(name);
            BuildReferences(reader, tracker);

            

            return null;
        }

        private Assembly LoadAssembly(AssemblyName reference)
        {
            if (AssemblyDataResolve != null)
            {
                AssemblyName name = reference.Clone() as AssemblyName;
                foreach (Delegate d in AssemblyDataResolve.GetInvocationList())
                {
                    byte[] data = (d as Func<AssemblyName, byte[]>)?.Invoke(name);
                    if (data != null)
                        return LoadAssembly(data);
                }

                foreach (Delegate d in AssemblyStreamResolve.GetInvocationList())
                {
                    Stream data = (d as Func<AssemblyName, Stream>)?.Invoke(name);
                    if (data != null)
                        return LoadAssembly(data);
                }
            }
            throw new FileNotFoundException($"Could not find Assembly: {reference.FullName}");
        }

    }
}

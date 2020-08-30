using System;
using System.Collections.Generic;
using System.Text;

namespace MimiTools.Sync.Storage
{
    public interface ITypedAccessor<T> : IAccessor
    {
        public ref T Field { get; }
        
        public ref readonly T ReadOnlyField { get; }

        public new IThreadSafeAccessible<T> Target { get; }

        public new T Value { get; set; }
    }
}

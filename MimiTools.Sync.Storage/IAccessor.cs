using System;
using System.Collections.Generic;
using System.Text;

namespace MimiTools.Sync.Storage
{
    public interface IAccessor : IDisposable
    {
        public bool IsReadOnly { get; }

        public Type Type { get; }

        public object Value { get; set; }

        public IThreadSafeAccessible Target { get; } 
    }
}

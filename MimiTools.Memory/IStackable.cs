using System;
using System.Collections.Generic;
using System.Text;

namespace MimiTools.Memory
{
    public interface IStackable<T> where T : class, IStackable<T>
    {
        public ref T Next { get; }

        public void Reset(bool live);
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace MimiTools.Sync
{
    public interface ILock : IDisposable
    {
        bool IsValid { get; }

        void Release();
    }
}

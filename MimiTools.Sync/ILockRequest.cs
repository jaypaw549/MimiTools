using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MimiTools.Sync
{
    public interface ILockRequest
    {
        bool IsCanceled { get; }

        bool IsCompleted { get; }

        bool IsGranted { get; }

        bool Cancel();

        ILock GetLock();

        Task<ILock> GetLockAsync();
        
        void Wait();

        Task WaitAsync();

        void OnCompleted(Action continuation);
    }
}

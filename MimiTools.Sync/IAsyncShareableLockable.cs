using System.Threading.Tasks;

namespace MimiTools.Sync
{
    public interface IAsyncShareableLockable : IAsyncLockable, IShareableLockable
    {
        Task<ILock> GetSharedLockAsync();

        ILockRequest RequestSharedLock();
    }
}
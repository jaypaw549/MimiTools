using MimiTools.Tools;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace MimiTools.Sync
{
    public static partial class AdvancedLockExtensions
    {
        public readonly struct LockAwaiter : INotifyCompletion
        {
            internal LockAwaiter(AdvancedLock.LockRequest request)
            {
                _request = request;
            }

            private readonly AdvancedLock.LockRequest _request;

            public bool IsCompleted => !_request.IsPending; //Check against pending, so that we can prevent deadlock on cancel or release

            public AdvancedLock.Lock GetResult()
                => _request.GetLock();

            public void OnCompleted(Action continuation)
                => _request.OnCancelledOrGranted(continuation);
        }

        public readonly struct ConfiguredLockAwaitable
        {
            private readonly bool _bind;
            private readonly AdvancedLock.LockRequest _request;

            internal ConfiguredLockAwaitable(AdvancedLock.LockRequest request, bool bind)
            {
                _bind = bind;
                _request = request;
            }

            public ConfiguredLockAwaiter GetAwaiter()
                => new ConfiguredLockAwaiter(_request, _bind);
        }

        public readonly struct ConfiguredLockAwaiter : INotifyCompletion
        {
            private readonly bool _bind;
            private readonly AdvancedLock.LockRequest m_request;

            internal ConfiguredLockAwaiter(AdvancedLock.LockRequest request, bool bind)
            {
                _bind = bind;
                m_request = request;
            }

            public bool IsCompleted => !m_request.IsPending;

            public AdvancedLock.Lock GetResult()
            {
                AdvancedLock.Lock l = m_request.GetLock();

                if (_bind)
                    l.Bind();

                return m_request.GetLock();
            }

            public void OnCompleted(Action continuation)
                => m_request.OnCancelledOrGranted(continuation);
        }

        public readonly struct UpgradeAwaiter : INotifyCompletion
        {
            internal UpgradeAwaiter(AdvancedLock.UpgradeRequest request)
            {
                _request = request;
            }

            private readonly AdvancedLock.UpgradeRequest _request;

            public bool IsCompleted => !_request.IsPending;

            public void GetResult()
                => _request.Wait();

            public void OnCompleted(Action continuation)
                => _request.OnCompleted(continuation);
        }

        public readonly struct VoidAwaitable
        {
            private readonly AdvancedLock.LockRequest _request;
            internal VoidAwaitable(AdvancedLock.LockRequest request)
            {
                _request = request;
            }

            public VoidAwaiter GetAwaiter()
                => new VoidAwaiter(_request);
        }

        public readonly struct VoidAwaiter : INotifyCompletion
        {
            private readonly AdvancedLock.LockRequest m_request;
            internal VoidAwaiter(AdvancedLock.LockRequest req)
            {
                m_request = req;
            }

            public bool IsCompleted => !m_request.IsPending;

            public void GetResult()
                => m_request.Wait();

            public void OnCompleted(Action continuation)
                => m_request.OnCancelledOrGranted(continuation);
        }

        private static void Bind(ref AdvancedLock.Lock l)
            => l.Bind();

        public static ConfiguredLockAwaitable ConfigureBind(this AdvancedLock.LockRequest request, bool bind)
            => new ConfiguredLockAwaitable(request, bind);

        /// <summary>
        /// Configures the binding status of the lock when acquired. A bound lock cannot be passed between sibling tasks/threads,
        /// but child tasks can also acquire the lock if they want.
        /// </summary>
        /// <param name="task">The task that will return an unbound lock</param>
        /// <param name="bind">whether or not to bind the lock</param>
        /// <returns>an awaitable object that will properly bind the returend lock if configured</returns>
        public static TaskResultFixup<AdvancedLock.Lock> ConfigureBind(this Task<AdvancedLock.Lock> task, bool bind)
            => new TaskResultFixup<AdvancedLock.Lock>(task, bind ? new TaskResultFixupDelegate<AdvancedLock.Lock>(Bind) : null);

        public static ConfiguredTaskResultFixup<AdvancedLock.Lock> ConfigureBind(this ConfiguredTaskAwaitable<AdvancedLock.Lock> awaitable, bool bind)
            => new ConfiguredTaskResultFixup<AdvancedLock.Lock>(awaitable, bind ? new TaskResultFixupDelegate<AdvancedLock.Lock>(Bind) : null);

        /// <summary>
        /// Creates an awaitable that'll complete when the request is finished, either by being cancelled or granted.
        /// An error may be thrown by the awaiter if the request is cancelled, otherwise a lock will be returned by it.
        /// </summary>
        /// <param name="request">The request to wait on</param>
        /// <returns></returns>
        public static LockAwaiter GetAwaiter(this AdvancedLock.LockRequest request)
            => new LockAwaiter(request);

        /// <summary>
        /// Creates an awaiter that'll complete when the lock is finished upgrading.
        /// </summary>
        /// <param name="request">The upgrade request to wait on</param>
        /// <returns>An awaiter that represents an asynchronous upgrade</returns>
        public static UpgradeAwaiter GetAwaiter(this AdvancedLock.UpgradeRequest request)
            => new UpgradeAwaiter(request);

        /// <summary>
        /// Creates an awaitable that'll complete when the request is finished, either by being cancelled or granted.
        /// No errors are thrown, and no values are retrieved from this awaitable!
        /// </summary>
        /// <returns>An awaitable that completes when this request is granted or cancelled</returns>
        public static VoidAwaitable WaitAsync(this AdvancedLock.LockRequest request)
            => new VoidAwaitable(request);
    }
}

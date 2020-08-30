using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace MimiTools.Sync
{
    public static partial class ReentrantLockExtensions
    {
        public readonly struct ConfiguredLockAwaitable
        {
            private readonly bool _bind;
            private readonly ReentrantLock.LockRequest _request;

            internal ConfiguredLockAwaitable(ReentrantLock.LockRequest request, bool bind)
            {
                _bind = bind;
                _request = request;
            }

            public ConfiguredLockAwaiter GetAwaiter()
                => new ConfiguredLockAwaiter(_request, _bind);
        }

        public readonly struct ConfiguredLockAwaiter : ICriticalNotifyCompletion
        {
            private readonly bool _bind;
            private readonly ReentrantLock.LockRequest _request;

            internal ConfiguredLockAwaiter(ReentrantLock.LockRequest request, bool bind)
            {
                _bind = bind;
                _request = request;
            }

            public bool IsCompleted => !_request.IsPending;

            public ReentrantLock.Lock GetResult()
            {
                if (_bind)
                    _request.Bind();

                return _request.GetLock();
            }

            public void OnCompleted(Action continuation)
                => _request.OnCancelledOrGranted(continuation);

            public void UnsafeOnCompleted(Action continuation)
                => _request.OnCancelledOrGranted(continuation);
        }

        public readonly struct LockAwaiter : ICriticalNotifyCompletion
        {
            internal LockAwaiter(ReentrantLock.LockRequest request)
            {
                _request = request;
            }

            private readonly ReentrantLock.LockRequest _request;

            public bool IsCompleted => !_request.IsPending; //Check against pending, so that we can prevent deadlock on cancel or release

            public ReentrantLock.Lock GetResult()
                => _request.GetLock();

            public void OnCompleted(Action continuation)
                => _request.OnCancelledOrGranted(continuation);

            public void UnsafeOnCompleted(Action continuation)
                => _request.OnCancelledOrGranted(continuation);
        }

        public readonly struct VoidAwaitable
        {
            private readonly ReentrantLock.LockRequest _request;
            internal VoidAwaitable(ReentrantLock.LockRequest request)
            {
                _request = request;
            }

            public VoidAwaiter GetAwaiter()
                => new VoidAwaiter(_request);
        }

        public readonly struct VoidAwaiter : ICriticalNotifyCompletion
        {
            private readonly ReentrantLock.LockRequest _request;
            internal VoidAwaiter(ReentrantLock.LockRequest request)
            {
                _request = request;
            }

            public bool IsCompleted => !_request.IsPending;

            public void GetResult()
                => _request.Wait();

            public void OnCompleted(Action continuation)
                => _request.OnCancelledOrGranted(continuation);

            public void UnsafeOnCompleted(Action continuation)
                => _request.OnCancelledOrGranted(continuation);
        }

        public static ConfiguredLockAwaitable ConfigureBind(this ReentrantLock.LockRequest request, bool bind)
            => new ConfiguredLockAwaitable(request, bind);

        public static LockAwaiter GetAwaiter(this ReentrantLock.LockRequest request)
            => new LockAwaiter(request);

        /// <summary>
        /// Creates an awaitable that'll complete when the request is finished, either by being cancelled or granted.
        /// </summary>
        /// <returns>An awaitable that completes when this request is granted or cancelled</returns>
        public static VoidAwaitable WaitAsync(this ReentrantLock.LockRequest request)
            => new VoidAwaitable(request);
    }
}

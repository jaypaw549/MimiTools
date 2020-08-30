using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MimiTools.Sync
{
    public sealed partial class ReentrantLock
    {
        public readonly struct Lock : IBindableLock
        {
            public bool IsActive => _request?.IsReady ?? false;

            public bool IsValid => _request != null;

            internal Lock(LockRequest request)
            {
                _request = request;
            }   

            private readonly LockRequest _request;

            /// <summary>
            /// Binds the lock to the ExecutionContext, allowing recursive reentry into the lock.
            /// </summary>
            /// <returns>a copy of itself</returns>
            public Lock Bind()
            {
                _request?.Bind();
                return this;
            }

            public void Dispose()
                => _request?.Release();

            public bool Release()
                => _request?.Release() ?? false;

            void IBindableLock.Bind()
                => Bind();

            void ILock.Release()
                => Release();
        }

        public class LockRequest : IBindableLockRequest
        {
            internal LockRequest(ReentrantLock l, LockRequest parent)
            {
                _lock = l;
                _parent = parent;
            }

            private readonly ReentrantLock _lock;
            private readonly LockRequest _parent;

            private volatile Action _continue = null;
            private volatile int _state = RequestState.Pending;

            private volatile LockRequest _prev = null;
            private volatile LockRequest _next = null;

            public bool IsCancelled => _state == RequestState.Cancelled;

            public bool IsDisposed => _state == RequestState.Disposed;

            public bool IsPending => _state == RequestState.Pending;

            public bool IsReady => _state == RequestState.Bound || _state == RequestState.Granted;

            public bool IsReleased => _state == RequestState.Released || _state == RequestState.Disposed;

            internal LockRequest Parent => _parent;

            internal LockRequest Prev => _prev;

            internal LockRequest Next => _next;

            bool ILockRequest.IsCanceled => throw new NotImplementedException();

            bool ILockRequest.IsCompleted => throw new NotImplementedException();

            bool ILockRequest.IsGranted => throw new NotImplementedException();

            internal void Bind()
            {
                int state = Interlocked.CompareExchange(ref _state, RequestState.Bound, RequestState.Granted);
                if (state == RequestState.Granted)
                {
                    try
                    {
                        _lock.BindLock(this, false);
                    }
                    catch
                    {
                        Interlocked.CompareExchange(ref _state, RequestState.Granted, RequestState.Bound);
                        throw;
                    }
                }

                else if (state == RequestState.Bound)
                    _lock.BindLock(this, true);

                else
                    throw new InvalidOperationException();
            }

            /// <summary>
            /// Cancels the current request, fails if the request isn't pending.
            /// </summary>
            public void Cancel()
            {
                int state = Interlocked.CompareExchange(ref _state, RequestState.Cancelled, RequestState.Pending);

                //if (state == RequestState.Granted)
                //    state = Interlocked.CompareExchange(ref _state, RequestState.Cancelled, RequestState.Granted);

                if (state == RequestState.Released || state == RequestState.Disposed)
                    throw new InvalidOperationException();

                //This is based on the state it was in *before* we did the exchange.
                if (state == RequestState.Pending)
                {
                    _lock.Remove(this, false);
                    Interlocked.Exchange(ref _continue, null)?.Invoke();
                }
            }

            /// <summary>
            /// Gets the lock, waiting if the request hasn't been granted or cancelled yet.
            /// </summary>
            /// <returns>A lock</returns>
            /// <exception cref="InvalidOperationException"/>
            /// <exception cref="OperationCanceledException"/>
            public Lock GetLock()
            {
                Wait();

                return _state switch
                {
                    RequestState.Bound => GetLock(true),
                    RequestState.Cancelled => throw new OperationCanceledException(),
                    RequestState.Disposed => throw new InvalidOperationException(),
                    RequestState.Granted => GetLock(false),
                    RequestState.Released => throw new InvalidOperationException(),
                    _ => throw new IndexOutOfRangeException(),
                };
            }

            private Lock GetLock(bool bound)
            {
                if (bound)
                    _lock.BindLock(this, true);

                return new Lock(this);
            }

            internal bool Grant()
            {
                if (RequestState.Pending == Interlocked.CompareExchange(ref _state, RequestState.Granted, RequestState.Pending))
                {
                    ThreadPool.UnsafeQueueUserWorkItem(Callback, this);
                    return true;
                }
                return false;
            }

            public void OnCancelledOrGranted(Action continuation)
            {
                //Remove synchronous execution
                Action c = _continue;
                while (true)
                {
                    if (_state != RequestState.Pending)
                    {
                        continuation?.Invoke();
                        return;
                    }

                    Action ret = Interlocked.CompareExchange(ref _continue, c + continuation, c);
                    if (ret == c)
                        break;
                    c = ret;
                }
            }

            internal bool Release()
            {
                int state = Interlocked.CompareExchange(ref _state, RequestState.Released, RequestState.Bound);
                if (state == RequestState.Granted)
                    state = Interlocked.CompareExchange(ref _state, RequestState.Released, RequestState.Granted);

                if (state == RequestState.Bound || state == RequestState.Granted)
                {
                    _lock.Remove(this, state == RequestState.Bound);
                    return true;
                }
                return false;
            }

            internal void UnsafeAppend(LockRequest prev)
            {
                if (prev == null)
                    return;

                _next = prev._next;
                prev._next = this;
                _prev = prev;
            }

            internal void UnsafeInsertAsChild(LockRequest parent)
            {
                _prev = parent._prev;
                parent._prev = this;

                _next = parent;
                if (_prev != null)
                    _prev._next = this;
            }

            internal void UnsafeUnchain()
            {
                if (_prev != null)
                    _prev._next = _next;

                if (_next != null)
                    _next._prev = _prev;

                _prev = null;
                _next = null;
                Interlocked.CompareExchange(ref _state, RequestState.Disposed, RequestState.Released);
            }

            /// <summary>
            /// Wait for this request to finish, either by being cancelled or granted.
            /// </summary>
            public void Wait()
            {
                SpinWait wait = new SpinWait();
                while (_state == RequestState.Pending)
                    wait.SpinOnce();
            }

            private static void Callback(object obj)
                => Interlocked.Exchange(ref (obj as LockRequest)._continue, null)?.Invoke();

            IBindableLock IBindableLockRequest.GetLock()
                => GetLock();

            async Task<IBindableLock> IBindableLockRequest.GetLockAsync()
                => await this;

            ILock ILockRequest.GetLock()
                => GetLock();

            async Task<ILock> ILockRequest.GetLockAsync()
                => await this;

            async Task ILockRequest.WaitAsync()
                => await this.WaitAsync();

            void ILockRequest.OnCompleted(Action continuation)
                => OnCancelledOrGranted(continuation);

            bool ILockRequest.Cancel()
            {
                if (!IsCancelled)
                    Cancel();
                return IsCancelled;
            }
        }

        private static class RequestState
        {
            internal const int Bound = 2, Cancelled = -1, Disposed = -2, Granted = 1, Pending = 0, Released = 3;
        }
    }
}

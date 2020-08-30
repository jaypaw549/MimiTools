using MimiTools.Tools;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MimiTools.Sync
{
    /// <summary>
    /// A synchronization primitive meant to replace asynchronous locking. It is "Advanced" because maximum control is given to the user on how to manage waiting for the lock.
    /// It also supports asynchronous reentry.
    /// </summary>
    public sealed partial class ReentrantLock : IAsyncBindableLockable
    {
        /// <summary>
        /// Synchronization primitive, used for request queue management. Required for thread safety with the doubley linked queue.
        /// </summary>
        private ThreadSafe64 _safe = new ThreadSafe64();

        /// <summary>
        /// The currently active request. This will be the youngest child if there are any child lock holders.
        /// </summary>
        private volatile LockRequest _current = null;

        /// <summary>
        /// The last request in line. Any parent-less requests for the lock will be put here.
        /// </summary>
        private volatile LockRequest _last = null;

        /// <summary>
        /// The currently active request for this logical thread. This will be null if the lock isn't currently held or isn't bound.
        /// If a lock request is made while this is assigned, that request will be put in *front* of this, and granted if it's at the front of the line.
        /// This is needed to implement reentry.
        /// </summary>
        private readonly AsyncLocal<LockRequest> _local_current = new AsyncLocal<LockRequest>();

        /// <summary>
        /// Binds the current request to the stack, allowing reentrancy, but preventing it from being released by sibling tasks/threads.
        /// </summary>
        /// <param name="request">The request to bind to the current stack</param>
        /// <param name="bound">Whether or the request has been bound previously</param>
        private void BindLock(LockRequest request, bool bound)
        {
            if (!bound)
            {
                if (ReferenceEquals(request.Parent, _local_current.Value))
                    _local_current.Value = request;
                else
                    throw new InvalidOperationException("You cannot bind a child lock unless you've bound the parent!");
            }
            else if (!ReferenceEquals(_local_current.Value, request))
                throw new InvalidOperationException("This lock is bound by another task!");
        }

        /// <summary>
        /// Creates a <see cref="LockRequest"/>, which if not cancelled will eventually be granted. It can be awaited on, and doesn't propogate the stack on continuations.
        /// </summary>
        /// <returns>A lock request which can be cancelled and will otherwise eventually complete</returns>
        public LockRequest RequestLock()
        {
            LockRequest parent = _local_current.Value;
            LockRequest request = new LockRequest(this, parent);

            bool grant;
            if (parent != null)
                grant = _safe.Do(UnsafeInsertChild, (request, parent));
            else
                grant = _safe.Do(UnsafeAppend, request);

            if (grant)
                request.Grant();

            return request;
        }

        /// <summary>
        /// Attempts to get the lock, will not create a request unless necessary.
        /// </summary>
        /// <param name="l">The location to write the lock to.</param>
        /// <returns>True if the lock was acquired</returns>
        public bool TryGetLock(out Lock l)
        {
            LockRequest request = _safe.Do(UnsafeTryGetLock, _local_current.Value);
            if (request != null)
            {
                request.Grant();
                l = request.GetLock();
                return true;
            }
            l = default;
            return false;
        }

        private void Remove(LockRequest target, bool release) 
        {
            if (release)
            {
                LockRequest parent = target.Parent;

                while (parent?.IsDisposed ?? false)
                    parent = parent.Parent;

                if (_local_current.Value == target)
                    _local_current.Value = parent;

                else
                    throw new InvalidOperationException("This lock is bound by another task!");
            }

            _safe.Do(UnsafeRemove, target)?.Grant();
        }

        private bool UnsafeAppend(LockRequest request)
        {
            request.UnsafeAppend(_last);
            _last = request;
            if (_current == null)
            {
                _current = request;
                return true;
            }
            return false;
        }

        private bool UnsafeInsertChild((LockRequest, LockRequest) pair)
        {
            LockRequest child, parent;
            (child, parent) = pair;

            while (parent?.IsDisposed ?? false) //If the parent is disposed of
                parent = parent.Parent; //Let the child be adopted by its grandparent

            if (parent == null) //If the child couldn't be adopted, treat it as an adult.
                return UnsafeAppend(child);

            child.UnsafeInsertAsChild(parent);
            if (_current == parent)
            {
                _current = child;
                return true;
            }
            return false;
        }

        private LockRequest UnsafeRemove(LockRequest request)
        {
            if (request.IsReleased)
            {
                if (request != _current)
                    return null;
                _current = request.Next;
            }

            else if (request.IsCancelled && _current == request)
                _current = request.Next;

            if (request == _last)
                _last = request.Prev;

            request.UnsafeUnchain();

            if (_current != null)
            {
                if (_current.IsReleased)
                    return UnsafeRemove(_current);
                return _current;
            }

            return null;
        }

        private LockRequest UnsafeTryGetLock(LockRequest parent)
        {
            if (_current != null && _current != parent)
                return null;

            LockRequest request = new LockRequest(this, parent);

            if (parent != null)
                request.UnsafeInsertAsChild(parent);

            return _current = request;
        }

        IBindableLock IBindableLockable.GetLock()
            => RequestLock().GetLock();

        async Task<IBindableLock> IAsyncBindableLockable.GetLockAsync()
            => await RequestLock();

        async Task<ILock> IAsyncLockable.GetLockAsync()
            => await RequestLock();

        ILock ILockable.GetLock()
            => RequestLock().GetLock();

        IBindableLockRequest IAsyncBindableLockable.RequestLock()
            => RequestLock();

        ILockRequest IAsyncLockable.RequestLock()
            => RequestLock();
    }
}
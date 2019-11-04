using MimiTools.Extensions.Tasks;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MimiTools.Sync
{

    public sealed class QueuedAsyncSync : AsyncSync, IDisposable
    {
        private readonly ConcurrentQueue<Tuple<Delegate, TaskCompletionSource<int>>> _queue = new ConcurrentQueue<Tuple<Delegate, TaskCompletionSource<int>>>();

        public event Action ActionEnqueued;

        public Task AsyncOperationsTask { get; private set; }

        private Func<int, Task<int>> Executor;

        public override async Task Execute(Action a)
        {
            TaskCompletionSource<int> source = new TaskCompletionSource<int>();

            if (disposedValue)
                throw new ObjectDisposedException(typeof(QueuedAsyncSync).Name);
            _queue.Enqueue(Tuple.Create((Delegate)a, source));

            ActionEnqueued?.Invoke();
            await source.Task.ConfigureAwait(false);
        }

        public override async Task ExecuteAsync(Func<Task> f)
        {
            TaskCompletionSource<int> source = new TaskCompletionSource<int>();

            if (disposedValue)
                throw new ObjectDisposedException(typeof(QueuedAsyncSync).Name);
            _queue.Enqueue(Tuple.Create((Delegate)f, source));

            ActionEnqueued?.Invoke();
            await source.Task.ConfigureAwait(false);
        }

        public IReadOnlyCollection<Task<int>> GetTasks()
           => new ReadOnlyCollection<Task<int>>(_queue.Select(v => v.Item2.Task).ToList());

        public int GetQueueLength()
            => _queue.Count;

        public int Run(int count = 1) // Consider this to be an extremely expensive operation
            => Internal_Run(count);

        public async Task<int> RunAsync(int count = 1) // Assume that awaiting on this will take a long time
        {
            if (Executor == null)
            {
                TaskCompletionSource<Func<int, Task<int>>> exe = new TaskCompletionSource<Func<int, Task<int>>>();
                AsyncOperationsTask = Task.Factory.StartNew(() =>
                {
                    int n = 0;
                    AsyncSync sync = new AsyncSync();
                    AsyncWaiter flow_control = new AsyncWaiter();
                    TaskCompletionSource<int> result = new TaskCompletionSource<int>();
                    Task<int> execute(int number)
                    {
                        return sync.ExecuteAsync(async () =>
                        {
                            n = number;
                            await flow_control.PulseAsync(true);
                            try
                            {
                                number = await result.Task;
                            }
                            catch (Exception)
                            {
                                await Task.Yield();
                                result = new TaskCompletionSource<int>();
                                throw;
                            }

                            await Task.Yield();
                            result = new TaskCompletionSource<int>();
                            return number;
                        });
                    }

                    exe.SetResult(execute);

                    while (n >= 0)
                    {
                        flow_control.Wait();

                        if (n > 0)
                            try
                            {
                                result.SetResult(Internal_Run(n));
                            }
                            catch (Exception e)
                            {
                                result.SetException(e);
                            }
                        else
                            result.SetResult(0);
                    }
                }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
                Interlocked.Exchange(ref Executor, await exe.Task)?.Invoke(-1);
                await Task.Yield();
            }

            return await Executor(count);
        }

        private int Internal_Run(int count) // Runs the tasks on whatever thread calls this
        {
            int i = 0;
            using (sync.GetLock())
            {
                if (disposedValue)
                    throw new ObjectDisposedException(typeof(QueuedAsyncSync).Name);

                while (i++ < count && _queue.Count > 0)
                {
                    if (!_queue.TryDequeue(out Tuple<Delegate, TaskCompletionSource<int>> entry))
                    {
                        i--;
                        Thread.Yield();
                        continue;
                    }

                    try
                    {
                        if (entry.Item1 is Action a)
                            new SingleThreadedOperation(a).Execute();
                        else if (entry.Item1 is Func<Task> f)
                            new SingleThreadedOperation(f).Execute();
                        entry.Item2.SetResult(i);
                    }
                    catch (Exception e)
                    {
                        entry.Item2.SetException(e);
                    }
                }
                return --i;
            }
        }

        private class SingleThreadedOperation
        {
            internal SingleThreadedContext Context { get; }
            internal TaskFactory Factory { get; }
            internal SingleThreadedTaskScheduler Scheduler { get; }
            internal BlockingCollection<Task> Tasks { get; }

            private readonly Delegate Operation;

            internal SingleThreadedOperation(Func<Task> f)
            {
                Context = new SingleThreadedContext(this);
                Scheduler = new SingleThreadedTaskScheduler(this);
                Tasks = new BlockingCollection<Task>();
                Factory = new TaskFactory(CancellationToken.None, TaskCreationOptions.HideScheduler, TaskContinuationOptions.HideScheduler, Scheduler);
                Operation = f;
            }

            internal SingleThreadedOperation(Action a)
            {
                Context = new SingleThreadedContext(this);
                Scheduler = new SingleThreadedTaskScheduler(this);
                Tasks = new BlockingCollection<Task>();
                Factory = new TaskFactory(CancellationToken.None, TaskCreationOptions.HideScheduler, TaskContinuationOptions.HideScheduler, Scheduler);
                Operation = a;
            }

            internal int AsynchronousOperations { get => _async_ops; }

            private volatile int _async_ops = 0;

            internal void Enqueue(Task t)
            {
                OperationStarted();
                t.ContinueWith(x => OperationCompleted(), TaskContinuationOptions.ExecuteSynchronously);
                if (!Tasks.TryAdd(t))
                    t.Start(TaskScheduler.Default);
            }

            public void Execute()
            {
                if (Operation is Action a)
                    Execute(a);
                else if (Operation is Func<Task> f)
                    Execute(f);
            }

            private void Execute(Action a)
                => Run(Factory.Run(a));

            private void Execute(Func<Task> f)
            {
                OperationStarted();
                Run(Factory.Run(f).ContinueWith(t =>
                {
                    OperationCompleted();
                    t.WaitAndUnwrapException();
                }, TaskContinuationOptions.ExecuteSynchronously));
            }

            internal void OperationCompleted()
            {
                int value = Interlocked.Decrement(ref _async_ops);
                if (value == 0)
                    Tasks.CompleteAdding();
            }

            internal void OperationStarted()
                => Interlocked.Increment(ref _async_ops);

            private void Run(Task target)
            {
                SynchronizationContext c = SynchronizationContext.Current;
                SynchronizationContext.SetSynchronizationContext(Context);
                try
                {
                    foreach (Task t in Tasks.GetConsumingEnumerable())
                        Scheduler.DoTryExecuteTask(t);

                    target.WaitAndUnwrapException();
                }
                catch
                {
                    SynchronizationContext.SetSynchronizationContext(c);
                    throw;
                }
                SynchronizationContext.SetSynchronizationContext(c);
            }
        }

        private class SingleThreadedContext : SynchronizationContext
        {
            private readonly SingleThreadedOperation Control;

            internal SingleThreadedContext(SingleThreadedOperation control)
            {
                Control = control;
            }

            public override void OperationCompleted()
                => Control.OperationCompleted();

            public override void OperationStarted()
                => Control.OperationStarted();

            public override void Post(SendOrPostCallback d, object state)
            {
                if (Control.Tasks.IsAddingCompleted) // If the function is complete, just queue it to the threadpool
                    ThreadPool.QueueUserWorkItem(o => d(o), state);
                else // If the function isn't complete, associate the work with the function
                    Control.Factory.StartNew(() => d(state), Control.Factory.CreationOptions | TaskCreationOptions.DenyChildAttach);
            }

            public override void Send(SendOrPostCallback d, object state)
            {
                if (Current == Control.Context) // We're in the correct context, run it immediately
                    d(state);
                else if (Control.Tasks.IsAddingCompleted) // Function is "Complete", Forget the synchronization context, just queue it to the threadpool
                    ThreadPool.QueueUserWorkItem(o => d(o), state);
                else // Function isn't complete, and we aren't on the right thread. Queue the work to the function and wait for it to execute and finish
                    Control.Factory.StartNew(() => d(state), Control.Factory.CreationOptions | TaskCreationOptions.DenyChildAttach).WaitAndUnwrapException();
            }
        }

        private class SingleThreadedTaskScheduler : TaskScheduler
        {
            private readonly SingleThreadedOperation Control;

            internal SingleThreadedTaskScheduler(SingleThreadedOperation control)
            {
                Control = control;
            }

            public override int MaximumConcurrencyLevel => 1;

            protected override IEnumerable<Task> GetScheduledTasks()
                => new ReadOnlyCollection<Task>(Control.Tasks.ToList());

            protected override void QueueTask(Task task)
                => Control.Enqueue(task);

            protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
                => Control.Context == SynchronizationContext.Current && TryExecuteTask(task);

            internal void DoTryExecuteTask(Task t)
                => TryExecuteTask(t);

        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                Executor?.Invoke(-1);
                Executor = null;

                Task.Run(async () =>
                {
                    while (_queue.Count > 0)
                    {
                        if (!_queue.TryDequeue(out var result))
                        {
                            await Task.Yield();
                            continue;
                        }

                        result.Item2.TrySetCanceled();
                    }
                });

                disposedValue = true;
            }
        }

        ~QueuedAsyncSync()
        {
            Dispose(false);
        }

        void IDisposable.Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}

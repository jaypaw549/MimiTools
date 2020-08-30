using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace MimiTools.Tools
{
    public delegate void TaskResultFixupDelegate<T>(ref T result);

    public readonly struct ConfiguredTaskResultFixup<T> : ICustomAwaitable<ConfiguredTaskResultFixupAwaiter<T>, T>
    {
        public ConfiguredTaskResultFixup(ConfiguredTaskAwaitable<T> awaitable, TaskResultFixupDelegate<T> fixup)
        {
            this.awaitable = awaitable;
            this.fixup = fixup;
        }

        private readonly TaskResultFixupDelegate<T> fixup;
        private readonly ConfiguredTaskAwaitable<T> awaitable;

        public ConfiguredTaskResultFixupAwaiter<T> GetAwaiter()
            => new ConfiguredTaskResultFixupAwaiter<T>(awaitable.GetAwaiter(), fixup);
    }

    public readonly struct ConfiguredTaskResultFixupAwaiter<T> : IAwaiter<T>
    {
        private readonly ConfiguredTaskAwaitable<T>.ConfiguredTaskAwaiter configuredTaskAwaiter;
        private readonly TaskResultFixupDelegate<T> fixup;

        internal ConfiguredTaskResultFixupAwaiter(ConfiguredTaskAwaitable<T>.ConfiguredTaskAwaiter configuredTaskAwaiter, TaskResultFixupDelegate<T> fixup)
        {
            this.configuredTaskAwaiter = configuredTaskAwaiter;
            this.fixup = fixup;
        }

        public bool IsCompleted => configuredTaskAwaiter.IsCompleted;

        public T GetResult()
        {
            T result = configuredTaskAwaiter.GetResult();
            fixup?.Invoke(ref result);
            return result;
        }

        public void OnCompleted(Action continuation)
            => configuredTaskAwaiter.OnCompleted(continuation);

        public void UnsafeOnCompleted(Action continuation)
            => configuredTaskAwaiter.OnCompleted(continuation);
    }

    public readonly struct TaskResultFixup<T> : ICustomAwaitable<TaskResultFixupAwaiter<T>, T>
    {
        public TaskResultFixup(Task<T> task, TaskResultFixupDelegate<T> fixup)
        {
            this.fixup = fixup;
            this.task = task ?? throw new ArgumentNullException(nameof(task));
        }

        private readonly TaskResultFixupDelegate<T> fixup;
        private readonly Task<T> task;

        public ConfiguredTaskResultFixup<T> ConfigureAwait(bool continueOnCapturedContext)
            => new ConfiguredTaskResultFixup<T>(task.ConfigureAwait(continueOnCapturedContext), fixup);

        public TaskResultFixupAwaiter<T> GetAwaiter()
            => new TaskResultFixupAwaiter<T>(task.GetAwaiter(), fixup);
    }

    public readonly struct TaskResultFixupAwaiter<T> : IAwaiter<T>, ICriticalNotifyCompletion
    {
        private readonly TaskResultFixupDelegate<T> fixup;
        private readonly TaskAwaiter<T> taskAwaiter;

        internal TaskResultFixupAwaiter(TaskAwaiter<T> taskAwaiter, TaskResultFixupDelegate<T> fixup)
        {
            this.fixup = fixup;
            this.taskAwaiter = taskAwaiter;
        }

        public bool IsCompleted => taskAwaiter.IsCompleted;

        public T GetResult()
        {
            T result = taskAwaiter.GetResult();
            fixup?.Invoke(ref result);
            return result;
        }

        public void OnCompleted(Action continuation)
            => taskAwaiter.OnCompleted(continuation);

        public void UnsafeOnCompleted(Action continuation)
            => taskAwaiter.OnCompleted(continuation);
    }
}

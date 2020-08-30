using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace MimiTools.Tools
{
    public readonly struct ConfiguredTaskResultModifier<TIn, TOut> : ICustomAwaitable<ConfiguredTaskResultModifierAwaiter<TIn, TOut>, TOut>
    {
        public ConfiguredTaskResultModifier(ConfiguredTaskAwaitable<TIn> awaitable, Func<TIn, TOut> modifier)
        {
            this.awaitable = awaitable;
            this.modifier = modifier ?? throw new ArgumentNullException(nameof(modifier));
        }

        private readonly Func<TIn, TOut> modifier;
        private readonly ConfiguredTaskAwaitable<TIn> awaitable;

        public ConfiguredTaskResultModifierAwaiter<TIn, TOut> GetAwaiter()
            => new ConfiguredTaskResultModifierAwaiter<TIn, TOut>(awaitable.GetAwaiter(), modifier);
    }

    public readonly struct ConfiguredTaskResultModifierAwaiter<TIn, TOut> : IAwaiter<TOut>
    {
        private readonly ConfiguredTaskAwaitable<TIn>.ConfiguredTaskAwaiter configuredTaskAwaiter;
        private readonly Func<TIn, TOut> modifier;

        internal ConfiguredTaskResultModifierAwaiter(ConfiguredTaskAwaitable<TIn>.ConfiguredTaskAwaiter configuredTaskAwaiter, Func<TIn, TOut> modifier)
        {
            this.configuredTaskAwaiter = configuredTaskAwaiter;
            this.modifier = modifier;
        }

        public bool IsCompleted => configuredTaskAwaiter.IsCompleted;

        public TOut GetResult()
            => modifier(configuredTaskAwaiter.GetResult());

        public void OnCompleted(Action continuation)
            => configuredTaskAwaiter.OnCompleted(continuation);

        public void UnsafeOnCompleted(Action continuation)
            => configuredTaskAwaiter.OnCompleted(continuation);
    }

    public readonly struct TaskResultModifier<TIn, TOut> : ICustomAwaitable<TaskResultModifierAwaiter<TIn, TOut>, TOut>
    {
        public TaskResultModifier(Task<TIn> task, Func<TIn, TOut> modifier)
        {
            this.modifier = modifier ?? throw new ArgumentNullException(nameof(modifier));
            this.task = task;
        }

        private readonly Func<TIn, TOut> modifier;
        private readonly Task<TIn> task;

        public ConfiguredTaskResultModifier<TIn, TOut> ConfigureAwait(bool continueOnCapturedContext)
            => new ConfiguredTaskResultModifier<TIn, TOut>(task.ConfigureAwait(continueOnCapturedContext), modifier);

        public TaskResultModifierAwaiter<TIn, TOut> GetAwaiter()
            => new TaskResultModifierAwaiter<TIn, TOut>(task.GetAwaiter(), modifier);
    }

    public readonly struct TaskResultModifierAwaiter<TIn, TOut> : IAwaiter<TOut>, ICriticalNotifyCompletion
    {
        private readonly Func<TIn, TOut> modifier;
        private readonly TaskAwaiter<TIn> taskAwaiter;

        internal TaskResultModifierAwaiter(TaskAwaiter<TIn> taskAwaiter, Func<TIn, TOut> modifier)
        {
            this.modifier = modifier;
            this.taskAwaiter = taskAwaiter;
        }

        public bool IsCompleted => taskAwaiter.IsCompleted;

        public TOut GetResult()
            => modifier(taskAwaiter.GetResult());

        public void OnCompleted(Action continuation)
            => taskAwaiter.OnCompleted(continuation);

        public void UnsafeOnCompleted(Action continuation)
            => taskAwaiter.OnCompleted(continuation);
    }
}

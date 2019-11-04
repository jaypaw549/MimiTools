using System;
using System.Threading.Tasks;

namespace MimiTools.Sync
{
    public class PriorityAsyncSync : AsyncSync
    {
        private readonly AsyncSync InnerSync = new AsyncSync();

        public override Task Execute(Action a)
            => base.ExecuteAsync(() => InnerSync.Execute(a));

        public override Task ExecuteAsync(Func<Task> f)
            => base.ExecuteAsync(() => InnerSync.ExecuteAsync(f));

        public Task PriorityExecute(Action a)
            => InnerSync.Execute(a);

        public Task<T> PriorityExecute<T>(Func<T> f)
            => InnerSync.Execute(f);

        public Task PriorityExecuteAsync(Func<Task> f)
            => InnerSync.ExecuteAsync(f);

        public Task<T> PriorityExecuteAsync<T>(Func<Task<T>> f)
            => InnerSync.ExecuteAsync(f);
    }
}

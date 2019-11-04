using System;
using System.Threading.Tasks;

namespace MimiTools.Extensions.Tasks
{
    public static class TaskFactoryExtensions
    {
        public static Task Run(this TaskFactory factory, Action a)
            => factory.StartNew(a, factory.CancellationToken, TaskCreationOptions.DenyChildAttach | factory.CreationOptions, factory.Scheduler);

        public static Task<T> Run<T>(this TaskFactory factory, Func<T> f)
            => factory.StartNew(f, factory.CancellationToken, TaskCreationOptions.DenyChildAttach | factory.CreationOptions, factory.Scheduler);

        public static Task Run(this TaskFactory factory, Func<Task> f)
            => factory.StartNew(f, factory.CancellationToken, TaskCreationOptions.DenyChildAttach | factory.CreationOptions, factory.Scheduler).Unwrap();

        public static Task<T> Run<T>(this TaskFactory factory, Func<Task<T>> f)
            => factory.StartNew(f, factory.CancellationToken, TaskCreationOptions.DenyChildAttach | factory.CreationOptions, factory.Scheduler).Unwrap();
    }
}

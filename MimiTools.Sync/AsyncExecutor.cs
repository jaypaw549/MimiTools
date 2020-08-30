using System;
using System.Threading.Tasks;

namespace MimiTools.Sync
{
    /// <summary>
    /// Allows for synchronized execution of code inside an asynchronous environment
    /// </summary>
    public class AsyncExecutor
    {
        protected readonly ReentrantLock sync = new ReentrantLock();

        /// <summary>
        /// Enqueues the Action, executing it after previously queued actions and functions complete. Runs Synchronously if no other tasks are queued.
        /// This method is meant to be used for thread safety in an Asynchronous Context.
        /// </summary>
        /// <param name="a">The action to perform</param>
        /// <returns></returns>
        public async virtual Task Execute(Action a)
        {
            using (await sync.RequestLock().ConfigureBind(true))
                a();
        }

        /// <summary>
        /// Enqueues the Function, executing it after previously queued actions and functions complete. Runs Synchronously if no other tasks are queued.
        /// This method is meant to be used for thread safety in an Asynchronous Context.
        /// </summary>
        /// <param name="f">The function to get the result of</param>
        /// <returns>The result of the function</returns>
        public virtual async Task<T> Execute<T>(Func<T> f)
        {
            using (await sync.RequestLock().ConfigureBind(true))
                return f();
        }

        /// <summary>
        /// Enqueues the function, executing it after previously queued actions and functions complete. It awaits on the task it returns
        /// This method is meant to be used for thread safety in an Asynchronous Context.
        /// </summary>
        /// <param name="f">The function to get the result of</param>
        /// <returns>The result of the function</returns>
        public virtual async Task ExecuteAsync(Func<Task> f)
        {
            using (await sync.RequestLock().ConfigureBind(true))
                await f().ConfigureAwait(false);
        }

        /// <summary>
        /// Enqueues the function, executing it after previously queued actions and functions complete. It awaits on the task the provided function returns.
        /// This method is meant to be used for thread safety in an Asynchronous Context.
        /// </summary>
        /// <param name="f">The function to get the result of</param>
        /// <returns>The result of the function</returns>
        public virtual async Task<T> ExecuteAsync<T>(Func<Task<T>> f)
        {
            using (await sync.RequestLock().ConfigureBind(true))
                return await f().ConfigureAwait(false);
        }
    }
}

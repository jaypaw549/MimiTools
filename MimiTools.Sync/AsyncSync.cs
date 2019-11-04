using System;
using System.Threading.Tasks;

namespace MimiTools.Sync
{
    /// <summary>
    /// AsyncSync Class, Allows for Synchronized execution of code inside an asynchronous environment
    /// </summary>
    public class AsyncSync
    {
        protected readonly SyncLock sync = new SyncLock();

        /// <summary>
        /// Enqueues the Action, executing it after previously queued actions and functions complete. Runs Synchronously if no other tasks are queued.
        /// This method is meant to be used for thread safety in an Asynchronous Context.
        /// </summary>
        /// <param name="a">The action to perform</param>
        /// <returns></returns>
        public virtual async Task Execute(Action a)
        {
            using (await sync.GetLockAsync().ConfigureAwait(false))
                a();
        }

        /// <summary>
        /// Enqueues the Function, executing it after previously queued actions and functions complete. Runs Synchronously if no other tasks are queued.
        /// This method is meant to be used for thread safety in an Asynchronous Context.
        /// </summary>
        /// <param name="f">The function to get the result of</param>
        /// <returns>The result of the function</returns>
        public async Task<T> Execute<T>(Func<T> f)
        {
            T result = default;
            await Execute(delegate { result = f(); }).ConfigureAwait(false);
            return result;
        }

        /// <summary>
        /// Enqueues the function, executing it after previously queued actions and functions complete. It awaits on the task it returns
        /// This method is meant to be used for thread safety in an Asynchronous Context.
        /// </summary>
        /// <param name="f">The function to get the result of</param>
        /// <returns>The result of the function</returns>
        public virtual async Task ExecuteAsync(Func<Task> f)
        {
            using (await sync.GetLockAsync().ConfigureAwait(false))
                await f().ConfigureAwait(false);
        }

        /// <summary>
        /// Enqueues the function, executing it after previously queued actions and functions complete. It awaits on the task the provided function returns.
        /// This method is meant to be used for thread safety in an Asynchronous Context.
        /// </summary>
        /// <param name="f">The function to get the result of</param>
        /// <returns>The result of the function</returns>
        public async Task<T> ExecuteAsync<T>(Func<Task<T>> f)
        {
            T result = default;
            await ExecuteAsync(async delegate () { result = await f(); }).ConfigureAwait(false);
            return result;
        }
    }
}

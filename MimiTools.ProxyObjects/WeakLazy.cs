using System;

namespace MimiTools.ProxyObjects
{
    /// <summary>
    /// A class that works with the ProxyGenerator to allow proxy implementations to be collected. Creates the object on demand,
    /// and doesn't prevent it from being garbage collected
    /// </summary>
    /// <typeparam name="T">The type of object to cache</typeparam>
    internal class WeakLazy<T> where T : class
    {
        internal WeakLazy(Func<T> factory)
            => _factory = factory;

        private readonly object _lock = new object();
        private readonly Func<T> _factory;
        private WeakReference<T> _value = null;

        internal T GetValue()
        {
            lock (_lock)
            {
                T value = default;
                if (!_value?.TryGetTarget(out value) ?? true)
                {
                    value = _factory();
                    _value = new WeakReference<T>(value);
                }
                return value;
            }
        }
    }
}

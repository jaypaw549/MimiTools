using System;
using System.Threading;

namespace MimiTools.Tools
{
    public class SynchronizationContextSwitcher : IDisposable
    {
        public static SynchronizationContextSwitcher NoContext { get => new SynchronizationContextSwitcher(null); }

        private readonly SynchronizationContext current, previous;
        private readonly bool force_switch;

        public SynchronizationContextSwitcher(SynchronizationContext context)
        {
            previous = SynchronizationContext.Current;
            current = context;
            SynchronizationContext.SetSynchronizationContext(context);
            force_switch = false;
        }

        public SynchronizationContextSwitcher(SynchronizationContext context, bool force_switch) : this(context)
        {
            this.force_switch = force_switch;
        }

        public void Dispose()
        {
            if (force_switch || SynchronizationContext.Current == current)
                SynchronizationContext.SetSynchronizationContext(previous);
        }
    }
}

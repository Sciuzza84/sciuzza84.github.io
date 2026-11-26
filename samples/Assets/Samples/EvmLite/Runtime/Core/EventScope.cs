using System;
using System.Collections.Generic;

namespace GameplaySamples.EvmLite
{
    /// <summary>Owns subscriptions that must end together, such as a scene's receivers.</summary>
    public sealed class EventScope : IDisposable
    {
        private readonly EventBus bus;
        private readonly List<Action> unsubscribe = new List<Action>();
        private bool disposed;

        internal EventScope(EventBus bus)
        {
            this.bus = bus;
        }

        public void Subscribe<T>(Action<T> handler)
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(EventScope));
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            Action cleanup = bus.Subscribe(this, handler);
            if (cleanup != null)
                unsubscribe.Add(cleanup);
        }

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            foreach (Action cleanup in unsubscribe)
                cleanup();
            unsubscribe.Clear();
        }
    }
}

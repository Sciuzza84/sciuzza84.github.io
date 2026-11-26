using System;
using System.Collections.Generic;

namespace GameplaySamples.EvmLite
{
    /// <summary>A synchronous, typed event bus for use from one thread.</summary>
    public sealed class EventBus
    {
        private readonly Dictionary<Type, object> channels = new Dictionary<Type, object>();

        public EventScope CreateScope()
        {
            return new EventScope(this);
        }

        public void Publish<T>(T message)
        {
            if (!channels.TryGetValue(typeof(T), out object channel))
                return;

            // Take a snapshot so a handler can add or dispose scopes during delivery.
            // Disposed subscriptions are skipped even when they were in that snapshot.
            var subscriptions = (List<Subscription<T>>)channel;
            foreach (Subscription<T> subscription in subscriptions.ToArray())
            {
                if (subscription.Active)
                    subscription.Handler(message);
            }
        }

        internal Action Subscribe<T>(EventScope scope, Action<T> handler)
        {
            Type eventType = typeof(T);
            if (!channels.TryGetValue(eventType, out object channel))
            {
                channel = new List<Subscription<T>>();
                channels.Add(eventType, channel);
            }

            var subscriptions = (List<Subscription<T>>)channel;
            foreach (Subscription<T> existing in subscriptions)
            {
                if (existing.Scope == scope && existing.Handler == handler)
                    return null;
            }

            var subscription = new Subscription<T>(scope, handler);
            subscriptions.Add(subscription);
            return () =>
            {
                subscription.Active = false;
                subscriptions.Remove(subscription);
                if (subscriptions.Count == 0)
                    channels.Remove(eventType);
            };
        }

        private sealed class Subscription<T>
        {
            public readonly EventScope Scope;
            public readonly Action<T> Handler;
            public bool Active = true;

            public Subscription(EventScope scope, Action<T> handler)
            {
                Scope = scope;
                Handler = handler;
            }
        }
    }
}

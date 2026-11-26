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

            // Keep this dispatch's order while nested publishes can see newer membership.
            Subscription<T>[] snapshot = ((Channel<T>)channel).Snapshot;
            foreach (Subscription<T> subscription in snapshot)
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
                channel = new Channel<T>();
                channels.Add(eventType, channel);
            }

            var typedChannel = (Channel<T>)channel;
            Subscription<T> subscription = typedChannel.Add(scope, handler);
            if (subscription == null)
                return null;

            return () =>
            {
                typedChannel.Remove(subscription);
                if (typedChannel.Count == 0)
                    channels.Remove(eventType);
            };
        }

        private sealed class Channel<T>
        {
            private readonly List<Subscription<T>> subscriptions = new List<Subscription<T>>();
            private Subscription<T>[] snapshot;

            public int Count => subscriptions.Count;
            public Subscription<T>[] Snapshot => snapshot ??= subscriptions.ToArray();

            public Subscription<T> Add(EventScope scope, Action<T> handler)
            {
                foreach (Subscription<T> existing in subscriptions)
                {
                    if (existing.Scope == scope && existing.Handler == handler)
                        return null;
                }

                var subscription = new Subscription<T>(scope, handler);
                subscriptions.Add(subscription);
                snapshot = null;
                return subscription;
            }

            public void Remove(Subscription<T> subscription)
            {
                subscription.Active = false;
                subscriptions.Remove(subscription);
                snapshot = null;
            }
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

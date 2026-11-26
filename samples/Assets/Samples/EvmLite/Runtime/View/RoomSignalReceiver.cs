using System;
using UnityEngine;

namespace GameplaySamples.EvmLite
{
    public readonly struct RoomSignal
    {
        public readonly int Sequence;

        public RoomSignal(int sequence)
        {
            Sequence = sequence;
        }
    }

    public sealed class RoomSignalReceiver : MonoBehaviour
    {
        private EventBus bus;
        private Action<RoomSignal> onSignal;
        private EventScope subscriptions;

        internal void Initialize(EventBus bus, Action<RoomSignal> onSignal)
        {
            if (bus == null)
                throw new ArgumentNullException(nameof(bus));
            if (onSignal == null)
                throw new ArgumentNullException(nameof(onSignal));
            if (this.bus == bus && this.onSignal == onSignal)
                return;

            StopListening();
            this.bus = bus;
            this.onSignal = onSignal;
            StartListening();
        }

        private void OnEnable() => StartListening();

        private void OnDisable() => StopListening();

        private void OnDestroy() => StopListening();

        private void StartListening()
        {
            if (!isActiveAndEnabled || bus == null || subscriptions != null)
                return;

            subscriptions = bus.CreateScope();
            subscriptions.Subscribe(onSignal);
        }

        private void StopListening()
        {
            subscriptions?.Dispose();
            subscriptions = null;
        }
    }
}

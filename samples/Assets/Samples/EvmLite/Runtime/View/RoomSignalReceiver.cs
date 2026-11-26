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
        private EventScope subscriptions;

        internal void Initialize(EventBus bus, Action<RoomSignal> onSignal)
        {
            subscriptions = bus.CreateScope();
            subscriptions.Subscribe(onSignal);
        }

        private void OnDestroy()
        {
            subscriptions?.Dispose();
        }
    }
}

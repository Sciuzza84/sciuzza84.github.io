using System.Collections.Generic;

namespace GameplaySamples.EvmLite
{
    internal enum DeliveryPhase
    {
        Outer,
        Inner
    }

    internal readonly struct DeliveryStep
    {
        internal DeliveryStep(DeliveryPhase phase, string action)
        {
            Phase = phase;
            Action = action;
        }

        internal DeliveryPhase Phase { get; }
        internal string Action { get; }
    }

    internal static class NestedDeliveryExercise
    {
        internal static IReadOnlyList<DeliveryStep> Run()
        {
            var bus = new EventBus();
            using EventScope first = bus.CreateScope();
            using EventScope removed = bus.CreateScope();
            using EventScope survivor = bus.CreateScope();
            using EventScope added = bus.CreateScope();
            var steps = new List<DeliveryStep>();

            void Receive(char receiver, DeliveryPhase phase) => steps.Add(new DeliveryStep(phase, $"{receiver} receives"));

            first.Subscribe<DeliveryPhase>(phase =>
            {
                Receive('A', phase);
                if (phase == DeliveryPhase.Inner)
                    return;

                removed.Dispose();
                added.Subscribe<DeliveryPhase>(signal => Receive('C', signal));
                steps.Add(new DeliveryStep(phase, "A removes B and subscribes C"));
                bus.Publish(DeliveryPhase.Inner);
                steps.Add(new DeliveryStep(phase, "A resumes"));
            });
            removed.Subscribe<DeliveryPhase>(phase => Receive('B', phase));
            survivor.Subscribe<DeliveryPhase>(phase => Receive('D', phase));
            bus.Publish(DeliveryPhase.Outer);
            return steps.AsReadOnly();
        }
    }
}

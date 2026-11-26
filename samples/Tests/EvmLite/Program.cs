using GameplaySamples.EvmLite;

internal static class Program
{
    private static int Main()
    {
        try
        {
            TypedChannelsStaySeparate();
            SubscriptionsAndDisposalAreIdempotent();
            LifetimeFollowsTwentyRoomChanges();
            HandlerChangesDuringPublishArePredictable();
            Console.WriteLine("PASS: typed delivery, duplicate subscriptions, 20 room changes and changes during dispatch.");
            return 0;
        }
        catch (InvalidOperationException error)
        {
            Console.Error.WriteLine($"FAIL: {error.Message}");
            return 1;
        }
    }

    private static void TypedChannelsStaySeparate()
    {
        var bus = new EventBus();
        using EventScope scope = bus.CreateScope();
        int hits = 0;
        scope.Subscribe<int>(value => hits += value);
        bus.Publish("42");
        bus.Publish(2);
        Require(hits == 2, "The callback received a payload from a different channel.");
    }

    private static void SubscriptionsAndDisposalAreIdempotent()
    {
        var bus = new EventBus();
        using EventScope first = bus.CreateScope();
        using EventScope second = bus.CreateScope();
        int hits = 0;
        Action<int> callback = _ => hits++;
        first.Subscribe(callback);
        first.Subscribe(callback);
        second.Subscribe(callback);

        bus.Publish(0);
        Require(hits == 2, "The same callback must be registered once per scope.");
        first.Dispose();
        first.Dispose();
        bus.Publish(0);
        Require(hits == 3, "Repeated disposal altered another scope.");

        try
        {
            first.Subscribe(callback);
        }
        catch (ObjectDisposedException)
        {
            return;
        }

        throw new InvalidOperationException("A disposed scope accepted a new subscription.");
    }

    private static void LifetimeFollowsTwentyRoomChanges()
    {
        var bus = new EventBus();
        using EventScope persistent = bus.CreateScope();
        int activeHits = 0;
        int persistentHits = 0;
        persistent.Subscribe<int>(_ => persistentHits++);

        EventScope activeRoom = bus.CreateScope();
        activeRoom.Subscribe<int>(_ => activeHits++);
        for (int transition = 1; transition <= 20; transition++)
        {
            activeRoom.Dispose();
            activeRoom = bus.CreateScope();
            activeRoom.Subscribe<int>(_ => activeHits++);
            bus.Publish(transition);
            Require(activeHits == transition, $"An unloaded receiver got event {transition}.");
            Require(persistentHits == transition, $"The persistent receiver missed event {transition}.");
        }

        activeRoom.Dispose();
        bus.Publish(21);
        Require(activeHits == 20 && persistentHits == 21, "Room cleanup removed the persistent receiver.");
    }

    private static void HandlerChangesDuringPublishArePredictable()
    {
        var bus = new EventBus();
        using EventScope first = bus.CreateScope();
        using EventScope removedDuringPublish = bus.CreateScope();
        using EventScope addedDuringPublish = bus.CreateScope();
        var trace = new List<string>();
        Action<int> late = _ => trace.Add("late");
        first.Subscribe<int>(_ =>
        {
            trace.Add("first");
            removedDuringPublish.Dispose();
            addedDuringPublish.Subscribe(late);
        });
        removedDuringPublish.Subscribe<int>(_ => trace.Add("removed"));

        bus.Publish(1);
        Require(trace.SequenceEqual(new[] { "first" }), "The first publish included a late or disposed handler.");
        trace.Clear();
        bus.Publish(2);
        Require(trace.SequenceEqual(new[] { "first", "late" }), "The second publish did not deliver in subscription order.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}

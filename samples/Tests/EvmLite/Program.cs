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
            NestedPublishUsesTheCurrentSubscriptionOrder();
            NestedPublishCanReplaceAnEmptyChannel();
            StablePublishDoesNotAllocate();
            Console.WriteLine("PASS: typed delivery, duplicate subscriptions, 20 room changes, reentrant dispatch and stable publishing.");
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

    private static void NestedPublishUsesTheCurrentSubscriptionOrder()
    {
        var bus = new EventBus();
        using EventScope first = bus.CreateScope();
        using EventScope removed = bus.CreateScope();
        using EventScope survivor = bus.CreateScope();
        using EventScope added = bus.CreateScope();
        var trace = new List<string>();

        first.Subscribe<int>(value =>
        {
            trace.Add($"first:{value}");
            if (value == 1)
            {
                removed.Dispose();
                added.Subscribe<int>(newValue => trace.Add($"added:{newValue}"));
                bus.Publish(2);
                trace.Add("first:resumed");
            }
        });
        removed.Subscribe<int>(value => trace.Add($"removed:{value}"));
        survivor.Subscribe<int>(value => trace.Add($"survivor:{value}"));
        bus.Publish(1);

        string[] expected = { "first:1", "first:2", "survivor:2", "added:2", "first:resumed", "survivor:1" };
        Require(trace.SequenceEqual(expected), $"Unexpected nested delivery order: {string.Join(", ", trace)}");
    }

    private static void NestedPublishCanReplaceAnEmptyChannel()
    {
        var bus = new EventBus();
        using EventScope original = bus.CreateScope();
        using EventScope replacement = bus.CreateScope();
        var trace = new List<string>();

        original.Subscribe<string>(value =>
        {
            trace.Add($"original:{value}");
            original.Dispose();
            replacement.Subscribe<string>(newValue => trace.Add($"replacement:{newValue}"));
            bus.Publish("inner");
        });

        bus.Publish("outer");
        bus.Publish("next");

        string[] expected = { "original:outer", "replacement:inner", "replacement:next" };
        Require(trace.SequenceEqual(expected), $"Unexpected replacement channel delivery: {string.Join(", ", trace)}");
    }

    private static void StablePublishDoesNotAllocate()
    {
        const int Publications = 10_000;
        var bus = new EventBus();
        using EventScope first = bus.CreateScope();
        using EventScope second = bus.CreateScope();
        int deliveries = 0;
        Action<int> count = value => deliveries += value;
        first.Subscribe(count);
        second.Subscribe(count);

        PublishMany(bus, Publications);
        int beforeDeliveries = deliveries;
        long beforeBytes = GC.GetAllocatedBytesForCurrentThread();
        PublishMany(bus, Publications);
        long allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - beforeBytes;

        Require(deliveries - beforeDeliveries == Publications * 2, "The allocation check lost deliveries.");
        Require(allocatedBytes == 0, $"Stable publishing allocated {allocatedBytes:N0} bytes.");
        Console.WriteLine($"Stable publishing: {allocatedBytes} managed bytes across {Publications:N0} warmed publications.");
    }

    private static void PublishMany(EventBus bus, int publications)
    {
        for (int index = 0; index < publications; index++)
            bus.Publish(1);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}

# EVM Lite

A scoped, typed event bus with a Unity scene-lifetime example. The design comes from Cristiano Piromalli’s work on event systems and additive loading. This example is a new implementation created with Codex.

## Try it

Open [EvmLite.unity](Scenes/EvmLite.unity) and press Play. With room A loaded, publish a signal. Load room B and publish again, then reload room A and publish once more. The latest signal should reach the loaded room and the persistent receiver once each. The unloaded room should receive zero deliveries. The cumulative totals retain earlier deliveries so you can compare successive runs.

The main scene stays loaded. Each room is an actual additive Unity scene with its own [receiver GameObject](Runtime/View/RoomSignalReceiver.cs). Destroying that GameObject during scene unload disposes the subscriptions it owns. The [demo controller](Runtime/View/EvmLiteExplorer.cs) owns the persistent receiver for the duration of the sample.

## Use the core

```csharp
var bus = new EventBus();
using var room = bus.CreateScope();
room.Subscribe<int>(signal => Console.WriteLine(signal));
bus.Publish(42);
```

Subscribers listen for an exact payload type. Registering the same delegate twice in one scope adds it once. Disposing the scope twice has the same effect as disposing it once, and subscribing to a disposed scope is an error.

The [bus](Runtime/Core/EventBus.cs) delivers synchronously in subscription order on a single thread. It snapshots that order per publish, then skips subscriptions disposed before their turn. A handler added during delivery participates in the next publish. Exceptions propagate to the publisher. Each snapshot allocates an array, so measure before choosing this design for high-frequency events.

## Verify

From the repository root, run `dotnet run --project Tests/EvmLite/EvmLite.Checks.csproj -c Release`. The [checks](../../../Tests/EvmLite/Program.cs) exercise exact typing, repeated room replacement, duplicate subscription and cleanup, and changes made by a handler during delivery. The Unity scene separately lets you observe the real scene load and unload.

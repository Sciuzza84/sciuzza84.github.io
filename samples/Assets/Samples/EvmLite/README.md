# EVM Lite

A scoped, typed event bus with a Unity scene-lifetime example. The design comes from Cristiano Piromalli’s work on event systems and additive loading. This example is a new implementation created with Codex.

## Try it

Open [EvmLite.unity](Scenes/EvmLite.unity) and press Play. With room A loaded, publish a signal. Load room B and publish again, then reload room A and publish once more. The latest signal should reach the loaded room and the persistent receiver once each. The unloaded room should receive zero deliveries. The cumulative totals retain earlier deliveries so you can compare successive runs.

The main scene stays loaded. Each room is an actual additive Unity scene with its own [receiver GameObject](Runtime/View/RoomSignalReceiver.cs). Destroying that GameObject during scene unload disposes the subscriptions it owns. The [demo controller](Runtime/View/EvmLiteExplorer.cs) owns the persistent receiver for the duration of the sample.

### Nested delivery

Choose **Nested delivery → Run nested trace**. The trace starts with listeners A, B and D, in registration order. While handling the outer publish, A disposes B, subscribes C and publishes the inner event. The inner publish reaches A, D and C. Then A resumes its outer callback, and that publish reaches D. Return to the additive room exercise with **Scene lifetime**.

The [trace runner](Runtime/Core/NestedDeliveryExercise.cs) runs an actual `EventBus` and records each callback and membership change. Each run starts with fresh scopes. The .NET test verifies the exact displayed order and a second identical run.

## Use the core

```csharp
var bus = new EventBus();
using var room = bus.CreateScope();
room.Subscribe<int>(signal => Console.WriteLine(signal));
bus.Publish(42);
```

Subscribers listen for an exact payload type. Registering the same delegate twice in one scope adds it once. Disposing the scope twice has the same effect as disposing it once, and subscribing to a disposed scope is an error.

The [bus](Runtime/Core/EventBus.cs) delivers synchronously in subscription order on a single thread. A publish holds its starting order and skips subscriptions disposed before their turn. A new handler joins subsequent publishes, including one started from inside a callback. The nested publish completes before the outer one resumes. Exceptions propagate to the publisher and stop the current dispatch.

Each event type caches a snapshot of its subscription list. Adding or removing a handler invalidates that type’s snapshot, and its next publish rebuilds it. Stable subscriptions reuse the array across sends. This keeps one extra reference per subscriber in the cached order and moves array creation to the first dispatch after a change. Scope setup, cleanup and handler work have their own allocation costs.

## Verify

From the repository root, run `dotnet run --project Tests/EvmLite/EvmLite.Checks.csproj -c Release`. The [checks](../../../Tests/EvmLite/Program.cs) cover exact typing, repeated room replacement, duplicate subscriptions, and nested publishes during scope changes. The .NET check also requires zero managed bytes over 10,000 warmed publishes with two stable subscribers and allocation-free callbacks. Profile your Unity player separately for its performance characteristics. The Unity scene lets you observe the real scene load and unload.

# Match recovery

An original scoreboard simulation inspired by the general problem of restoring a multiplayer client that has fallen behind. The model uses synthetic scores and locally simulated delivery. It uses no game source, network service or production data.

## Try it

1. **Deliver revision 1** and then **Skip delivery 2**. The authority advances twice and the client's score remains at revision 1.
2. **Deliver revision 3**. The client notices the gap and keeps the last complete score while waiting for a snapshot.
3. **Send older snapshot**. Revision 2 is rejected because the client has observed revision 3.
4. **Recover from snapshot** to restore revision 3 and the same score as the authority. **Replay last delivery** to confirm the score stays unchanged.

Open the scene in Unity or choose **Match recovery** from the Web gallery.

## Implementation and checks

The reducer applies the next consecutive event. Earlier events are idempotent and future events beyond a gap raise a minimum acceptable snapshot revision. One full state replaces the partial view when it covers that revision. The sample deliberately avoids a pending-event buffer. A production transport would also need an authoritative identity, retries and rules for reconnecting to a particular match.

[Replica reducer](Runtime/Core/MatchReplica.cs) · [Unity view](Runtime/View/MatchRecoveryExplorer.cs) · [Checks](../../../Tests/MatchRecovery/Program.cs)

Run the checks from the Unity project directory (`samples/` in the portfolio repository).

```sh
dotnet run --project Tests/MatchRecovery/MatchRecovery.Checks.csproj -c Release
```

# Turn-based combat

One Unity scene demonstrates turn order, opponent decisions, and pooling for floating combat labels. The rules run as plain C# and the view shows their results.

## Try it

- **End-phase effects.** At 2 health, take 2 damage at the end of the phase. The enemy response gets canceled. Use the 3 health preset and repeat to see one response. The 1 energy preset shows a blocked card leaving the phase open, with deck and energy intact.
- **Opponent choices.** In the recovery preset, resolve the action twice. The opponent waits to gain mana and then attacks. Switch to a preset with lower mana capacity to compare a living reserve with no useful move.
- **Feedback pool.** Select **Send burst**. Eight synthetic events fill four slots, merge three times and drop one unmatched event. Damage and healing at the same point get separate labels. Select **Advance tick** three times and send another burst to see four slots reused. The label numbers identify their slots.

The sample uses fictional cards and fixtures. The end-phase example draws on work Cristiano did to preserve the correct order of card effects and enemy responses in *Lost in the Dungeon*. The opponent example draws on refinements he made to tactical decisions in *Diamonst*. The explanation trace belongs to this demo.

The feedback example draws on Cristiano’s work on reuse and proximity-based aggregation for floating feedback in *Lost in the Dungeon*. The fixed capacity, simulated lifetime, counters and synthetic burst belong to this demo. Coalescing chooses the nearest active matching kind within the radius and keeps its anchor and expiry. An unmatched event drops when the pool is full.

## Run the checks

Open the [Combat scene](Scenes/Combat.unity) and press Play. Run the core checks from the Unity project directory (`samples/` in the portfolio repository).

    dotnet run --project Tests/Combat/Combat.Checks.csproj -c Release

[End-phase rules](Runtime/Core/EndPhaseRules.cs) · [Opponent choice rules](Runtime/Core/OpponentRules.cs) · [Feedback pool](Runtime/Core/FeedbackPool.cs) · [Checks](../../../Tests/Combat/Program.cs)

This opponent strategy deliberately looks ahead one turn. The fixed card deck illustrates action cost and rotation. Those boundaries keep the test cases small enough to understand from the trace.

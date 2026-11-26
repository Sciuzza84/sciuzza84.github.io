# Turn-based combat

One Unity scene demonstrates two decisions from turn-based games. The rules run as plain C# and the view shows the order of events and the reasons each move is available.

## Try it

- **End-phase effects.** At 2 health, take 2 damage at the end of the phase. The enemy response gets canceled. Use the 3 health preset and repeat to see one response. The 1 energy preset shows a blocked card leaving the phase open, with deck and energy intact.
- **Opponent choices.** In the recovery preset, resolve the action twice. The opponent waits to gain mana and then attacks. Switch to a preset with lower mana capacity to compare a living reserve with no useful move.

The sample uses fictional cards and fixtures. The end-phase example draws on work Cristiano did to preserve the correct order of card effects and enemy responses in *Lost in the Dungeon*. The opponent example draws on refinements he made to tactical decisions in *Diamonst*. The explanation trace belongs to this demo.

## Run the checks

Open the [Combat scene](Scenes/Combat.unity) and press Play. Run the core checks from the repository root:

    dotnet run --project Tests/Combat/Combat.Checks.csproj -c Release

[End-phase rules](Runtime/Core/EndPhaseRules.cs) · [Opponent choice rules](Runtime/Core/OpponentRules.cs) · [Checks](../../../Tests/Combat/Program.cs)

This opponent strategy deliberately looks ahead one turn. The fixed card deck illustrates action cost and rotation. Those boundaries keep the test cases small enough to understand from the trace.

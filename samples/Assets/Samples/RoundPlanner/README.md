# Round Planner

Build distinct target and distractor sets from a small catalog. This independent sample draws on Cristiano Piromalli’s round generation work for *Love, Diana Hidden Objects*. Its objects, variants and rules are synthetic.

## Try it

Open [RoundPlanner.unity](Scenes/RoundPlanner.unity) and press Play. Round 1 starts with Compass and Lantern as targets. Select **Try invalid quota** to request seven families from a six-family catalog. The accepted round and target counts stay as they were. **Replay accepted round** shows the same saved plan.

Select **Accept next round** twice. Key and Shell enter round 2, then Kite and Clock enter round 3. Every family now has one target appearance. Accept round 4 to see Compass and Lantern return with their other variants.

## Read and verify

[The planner](Runtime/Core/RoundPlanner.cs) ranks families by target frequency and variants by frequency within their family. Equal counts follow catalog order. Distractors use the next families in that order and leave target history unchanged. Validation runs before either the counters or the in-memory round changes. The accepted plan contains read-only lists.

From the Unity project directory (`samples/` in the portfolio repository) run `dotnet run --project Tests/RoundPlanner/RoundPlanner.Checks.csproj -c Release`. [Checks](../../../Tests/RoundPlanner/Program.cs) verify the four-round progression, distinct families, stable replay, rejected quotas, and repeatability.

# Dungeon generator

An original Unity sample inspired by work on the branching map and Editor validation in *Lost in the Dungeon*.

Open the Dungeon scene in Unity and press **Play**, or launch **Dungeon generator** from the Web gallery. Start with seed **19** and four branches. The map has 36 rooms and 35 links. The coral outline identifies the first rejected placement.

1. Open **Edit length weights** and choose **Invalid preset**. Row 2 duplicates the two-room choice and the probabilities add up to 80%. The map still uses the last applied weights.
2. Set the **Rooms** value in row 2 to `3` and the **Weight (%)** in row 3 to `90`. The total reaches 100% and **Apply weights** becomes available. On desktop, scroll inside the controls column to reach these fields.
3. Apply the weights and generate again with the same seed and branch count to reproduce the weighted map. **Restore default** returns to the original generation policy. **Valid preset** loads a ready-to-edit example.

The weights choose the initial straight length of a side path. Placement still needs grid space, so a candidate can be shortened or rejected. The main route uses the same seed regardless of the weights. Draft inputs take effect only after a valid preset is applied.

[Generator](Runtime/Core/DungeonGenerator.cs) · [Preset checks and sampling](Runtime/Core/DungeonBranchPreset.cs) · [Automated checks](../../../Tests/Dungeon/Program.cs)

Run the checks from the Unity project directory (`samples/` in the portfolio repository) with `dotnet run --project Tests/Dungeon/Dungeon.Checks.csproj -c Release`.

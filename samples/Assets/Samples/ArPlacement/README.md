# AR placement

This original study was inspired by Cristiano’s work on target setup and character spacing in *Diamonst*. The player simulates the two inputs that matter for initial placement, image quality and tilt. The drawing uses an anchor at the midpoint between two pieces. The coordinates are a model for placement in anchor-local space.

1. Press **Run replay**. Watch the progress bar start, reset when the tilt leaves the usable range and fill after the signal recovers.
2. Press **Confirm placement** once it becomes available. Use **Separation** and **Object scale**. The two origins move symmetrically when separation changes and stay fixed when only scale changes.
3. Press **Reset**. Use the sliders to try the thresholds manually.

The demo uses a continuous 1.5-second window at frame quality of at least 70% and tilt within ±30 degrees. This policy favors repeatable setup and makes a deliberate trade-off in startup time. The simulation shows setup and positioning without a camera feed. Its thresholds and replay are illustrative.

[Gate and anchor math](Runtime/Core/PlacementGate.cs) · [View and replay](Runtime/View/ArPlacementExplorer.cs) · [Checks](../../../Tests/ArPlacement/Program.cs)

From the repository root, run `dotnet run --project Tests/ArPlacement/ArPlacement.Checks.csproj -c Release`.

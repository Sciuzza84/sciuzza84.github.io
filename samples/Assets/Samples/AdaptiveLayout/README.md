# Adaptive Layout

Keep an anchored HUD inside the safe area and fit the whole playfield with an orthographic camera. This original example draws on Cristiano's work on layout tools across several Unity projects.

Open the **Adaptive Layout** scene and try portrait **9:16** with cutouts enabled. Toggle adaptation off to see the fixed-height camera crop the amber playfield and the HUD occupy the inset. Rotate to **19.5:9**, then return to portrait. The HUD and arena return to the same positions.

## Two layout policies

The HUD root is normalized against the intersection of the safe area and viewport. The world camera fits the wider requirement of world height or world width divided by the viewport aspect. Interactive HUD remains clear of cutouts, while the game can render behind them. Layout updates use the original bounds each time. An invalid resize preserves the previous valid configuration.

The scene previews these calculations with a simulated viewport. For an orthographic camera looking down the Z axis at the XY gameplay plane, call `UnityLayoutAdapter.TryApply(camera, safeRoot, worldBounds)` on initial layout and after the camera's pixel rectangle or `Screen.safeArea` changes. Attach the HUD items to the safe root and anchor them normally. The root must stretch inside a parent canvas that matches the camera viewport. The adapter leaves the current configuration in place if an input is empty during resizing.

## Checks

From the repository root run `dotnet run --project Tests/AdaptiveLayout/AdaptiveLayout.Checks.csproj -c Release`. The checks include asymmetric insets, a nonzero viewport origin, containment, repeated orientation changes, and transient invalid measurements.

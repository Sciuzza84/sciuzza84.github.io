# Adaptive Layout

Keep an anchored HUD inside the safe area and fit the whole playfield with an orthographic camera. This original example draws on Cristiano's work on layout tools across several Unity projects.

Open the **Adaptive Layout** scene and try portrait **9:16** with cutouts enabled. Toggle adaptation off to see the fixed-height camera crop the amber playfield and the HUD occupy the inset. Rotate to **19.5:9**, then return to portrait. The HUD and arena return to the same positions.

## Two layout policies

The HUD root is normalized against the intersection of the safe area and viewport in pixels. The world camera fits the larger of world height and world width divided by the projection aspect. The Unity adapter reads `Camera.aspect`, which can differ from the pixel viewport ratio. Standalone geometry callers use the viewport ratio by default. Layout updates use the original bounds each time. An invalid resize preserves the previous valid configuration.

The scene demonstrates these calculations with simulated viewports and cutouts.

## Set it up in a Unity scene

1. Use an orthographic camera aligned with the XY gameplay plane. For a playfield on `Z = 0`, position the camera at `Z = -10` with default rotation and viewport aspect.
2. Set the HUD Canvas to **Screen Space - Camera** and assign that camera. Add a stretched `SafeRoot` RectTransform directly inside the Canvas. Put the HUD elements under `SafeRoot` and anchor them there.
3. Add [Adaptive Layout Controller](Runtime/View/AdaptiveLayoutController.cs) to a scene GameObject. In the Inspector assign **Target Camera** and **Safe Root**. For a playfield 12 units wide and 7 high, set **World Bounds** to `X = -6`, `Y = -3.5`, `W = 12`, `H = 7`.
4. Enter Play and change the Game view aspect. The camera fits the same world bounds and keeps its Z position. The controller sets the root anchors from the overlap of the camera's viewport and the safe area.

The controller applies on enable and refreshes when the camera viewport, projection aspect, screen safe area, world bounds or assigned components change. Empty or disjoint samples preserve the last valid camera and anchors. Use one controller for each camera and HUD pair, with an axis-aligned orthographic camera using Unity’s standard projection on the main display. Call [UnityLayoutAdapter.TryApply](Runtime/View/UnityLayoutAdapter.cs) directly if you already manage layout updates in your own component.

## Checks

From the Unity project directory (`samples/` in the portfolio repository) run `dotnet run --project Tests/AdaptiveLayout/AdaptiveLayout.Checks.csproj -c Release`. The checks include asymmetric insets, a nonzero viewport origin, containment, repeated orientation changes, and transient invalid measurements.

Run the Unity Editor in batch mode with `-executeMethod GameplaySamples.AdaptiveLayout.Editor.AdaptiveLayoutVerification.VerifySample` to check an actual Camera and RectTransform with injected viewport and safe-area measurements. It verifies setup, invalid resize, rotation, steady inputs, and a projection-aspect change with the same pixel measurements.

# Unity portfolio samples

Interactive C# studies based on problems Cristiano Piromalli worked on as a Unity developer. The studies were created with Codex using new code, synthetic inputs, and simple graphics. [Portfolio](https://sciuzza84.github.io/) · [Source and tests](https://github.com/Sciuzza84/sciuzza84.github.io/tree/main/samples)

## Try the samples

Use a desktop browser with WebGL 2 for the Web player. [Open the gallery](https://sciuzza84.github.io/demos/unity/)

| Sample | What to try | Code and notes |
| --- | --- | --- |
| **Dungeon generator** | [Play](https://sciuzza84.github.io/demos/unity/?sample=dungeon) · Use seed **19** and four branches. Add a branch with **+** and look for the coral outline at the first rejected cell. | [Generator](Assets/Samples/Dungeon/Runtime/Core/DungeonGenerator.cs) · [Checks](Tests/Dungeon/Program.cs) · [Screenshot](Docs/dungeon-preview.png) |
| **AR placement** | [Play](https://sciuzza84.github.io/demos/unity/?sample=ar-placement) · Select **Run replay**, then **Confirm placement** when available. Change **Separation** and **Object scale** to see which coordinates change. | [Placement gate](Assets/Samples/ArPlacement/Runtime/Core/PlacementGate.cs) · [Guide](Assets/Samples/ArPlacement/README.md) · [Screenshot](Docs/ar-placement-preview.png) |
| **Content updates** | [Play](https://sciuzza84.github.io/demos/unity/?sample=content-updates) · Select **Version 2** and **Apply update**. The first update transfers one file and reuses two. Reapplying reuses all three. Switch to iOS and try **Corrupt file** to see v1 retained. | [Updater](Assets/Samples/ContentUpdates/Runtime/Core/ContentUpdater.cs) · [Guide](Assets/Samples/ContentUpdates/README.md) · [Screenshot](Docs/content-updates-preview.png) |
| **Level Editor** | In Unity, choose **Samples → Open Level Editor**, select **Gentle Ramp** or **Broken Bridge** and click **Copy to Draft**. Paint a tile and watch **Live Preview**. Save, edit again and reopen the draft. | [Editor window](Assets/Samples/LevelEditor/Editor/LevelEditorWindow.cs) · [Board rules](Assets/Samples/LevelEditor/Runtime/Core/LevelBoard.cs) · [Guide](Assets/Samples/LevelEditor/README.md) · [Screenshot](Docs/level-editor-preview.png) |
| **Turn-based combat** | [Play](https://sciuzza84.github.io/demos/unity/?sample=combat) · Resolve the end phase with two health and again with three. In **Opponent choices**, use the recovery preset and resolve twice to see the enemy wait and then attack. | [End-phase rules](Assets/Samples/Combat/Runtime/Core/EndPhaseRules.cs) · [Opponent rules](Assets/Samples/Combat/Runtime/Core/OpponentRules.cs) · [Guide](Assets/Samples/Combat/README.md) |
| **EVM Lite** | [Play](https://sciuzza84.github.io/demos/unity/?sample=evm-lite) · Use **Publish signal**, switch to room B and publish again. Return to room A to check that only the loaded room and the persistent receiver receive each signal. | [Event bus](Assets/Samples/EvmLite/Runtime/Core/EventBus.cs) · [Guide](Assets/Samples/EvmLite/README.md) |
| **Adaptive layout** | [Play](https://sciuzza84.github.io/demos/unity/?sample=adaptive-layout) · Toggle **Adaptation on** at **9:16** to compare the HUD and playfield. Re-enable adaptation and switch between **19.5:9** and **9:16**. | [Layout geometry](Assets/Samples/AdaptiveLayout/Runtime/Core/LayoutGeometry.cs) · [Guide](Assets/Samples/AdaptiveLayout/README.md) |

AR placement uses simulated tracking quality and tilt, Content Updates uses simulated transport and storage, and Adaptive Layout uses simulated screen insets.

## Open or build locally

Clone the portfolio repository, then open its `samples` folder with **Unity 6000.6.10f1**.

```sh
git clone https://github.com/Sciuzza84/sciuzza84.github.io.git
cd sciuzza84.github.io/samples
```

Open [Samples.unity](Assets/Samples/Gallery/Samples.unity) and press Play, or open Level Editor through the **Samples** menu.

To build for the browser, add **Web Build Support** for the same Unity version and choose **Samples → Build Gallery for Web**. Serve the build from the `samples` directory with Python 3, then [open the local gallery](http://127.0.0.1:8198/).

```sh
python -m http.server 8198 --bind 127.0.0.1 --directory Builds/Web/Gallery
```

## Run the checks

Run these commands from the `samples` directory with the **.NET 8 SDK** or a compatible newer SDK and runtime. Each check compiles the same C# core used by the Unity project.

```sh
dotnet run --project Tests/Dungeon/Dungeon.Checks.csproj -c Release
dotnet run --project Tests/ArPlacement/ArPlacement.Checks.csproj -c Release
dotnet run --project Tests/ContentUpdates/ContentUpdates.Checks.csproj -c Release
dotnet run --project Tests/LevelEditor/LevelEditor.Checks.csproj -c Release
dotnet run --project Tests/Combat/Combat.Checks.csproj -c Release
dotnet run --project Tests/EvmLite/EvmLite.Checks.csproj -c Release
dotnet run --project Tests/AdaptiveLayout/AdaptiveLayout.Checks.csproj -c Release
```

The [Level Editor check in Unity](Assets/Samples/LevelEditor/README.md) verifies saving and reloading a draft asset, including the preservation of its GUID.

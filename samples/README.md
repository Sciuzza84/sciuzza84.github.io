# Unity portfolio samples

Interactive C# studies based on problems Cristiano Piromalli worked on as a Unity developer. The studies were created with Codex using new code, synthetic inputs, and simple graphics. [Portfolio](https://sciuzza84.github.io/) · [Source and tests](https://github.com/Sciuzza84/sciuzza84.github.io/tree/main/samples)

## Try the samples

Use a desktop browser with WebGL 2 for the Web player. [Open the gallery](https://sciuzza84.github.io/demos/)

| Sample | What to try | Code and notes |
| --- | --- | --- |
| **Dungeon generator** | [Play](https://sciuzza84.github.io/demos/unity/?sample=dungeon) · Use seed **19** and four branches, then repair the invalid length preset. The current map stays visible while the draft is invalid. | [Generator](Assets/Samples/Dungeon/Runtime/Core/DungeonGenerator.cs) · [Guide](Assets/Samples/Dungeon/README.md) · [Checks](Tests/Dungeon/Program.cs) |
| **AR placement** | [Play](https://sciuzza84.github.io/demos/unity/?sample=ar-placement) · Select **Run replay**, then **Confirm placement** when available. Change **Separation** and **Object scale** to see which coordinates change. | [Placement gate](Assets/Samples/ArPlacement/Runtime/Core/PlacementGate.cs) · [Guide](Assets/Samples/ArPlacement/README.md) · [Checks](Tests/ArPlacement/Program.cs) |
| **Content updates** | [Play](https://sciuzza84.github.io/demos/unity/?sample=content-updates) · Apply **Version 2**, then try **Version 3** with **Corrupt file**. The second transfer fails after one file passes verification, and v2 stays installed. | [Updater](Assets/Samples/ContentUpdates/Runtime/Core/ContentUpdater.cs) · [Guide](Assets/Samples/ContentUpdates/README.md) · [Checks](Tests/ContentUpdates/Program.cs) |
| **Level Editor** | In Unity, choose **Samples → Open Level Editor**, select **Gentle Ramp** or **Broken Bridge** and click **Copy to Draft**. Paint a tile and watch **Live Preview**. Save, edit again and reopen the draft. | [Editor window](Assets/Samples/LevelEditor/Editor/LevelEditorWindow.cs) · [Guide](Assets/Samples/LevelEditor/README.md) · [Checks](Tests/LevelEditor/Program.cs) |
| **Turn-based combat** | [Play](https://sciuzza84.github.io/demos/unity/?sample=combat) · Compare end-phase health and opponent decisions. In **Feedback pool**, send a burst, advance three ticks and send another burst to see slot reuse. | [Rule resolution](Assets/Samples/Combat/Runtime/Core/EndPhaseRules.cs) · [Feedback pool](Assets/Samples/Combat/Runtime/Core/FeedbackPool.cs) · [Guide](Assets/Samples/Combat/README.md) · [Checks](Tests/Combat/Program.cs) |
| **EVM Lite** | [Play](https://sciuzza84.github.io/demos/unity/?sample=evm-lite) · Publish in Room A, switch to Room B and publish again. Try **Nested delivery** to see how a listener change affects inner and outer delivery. | [Event bus](Assets/Samples/EvmLite/Runtime/Core/EventBus.cs) · [Guide](Assets/Samples/EvmLite/README.md) · [Checks](Tests/EvmLite/Program.cs) |
| **Adaptive layout** | [Play](https://sciuzza84.github.io/demos/unity/?sample=adaptive-layout) · Toggle **Adaptation on** at **9:16** to compare the HUD and playfield. Re-enable adaptation and switch between **19.5:9** and **9:16**. | [Layout geometry](Assets/Samples/AdaptiveLayout/Runtime/Core/LayoutGeometry.cs) · [Guide](Assets/Samples/AdaptiveLayout/README.md) · [Checks](Tests/AdaptiveLayout/Program.cs) |
| **Layered layout** | [Play](https://sciuzza84.github.io/demos/unity/?sample=layered-layout) · Move the beacon, then try a naming or position error. The last valid preview stays visible. | [Prefab and data importer](Assets/Samples/LayeredLayout/Editor/LayeredLayoutImporter.cs) · [Guide](Assets/Samples/LayeredLayout/README.md) · [Checks](Tests/LayeredLayout/Program.cs) |
| **Round planner** | [Play](https://sciuzza84.github.io/demos/unity/?sample=round-planner) · Accept successive plans and compare target exposure. Try an infeasible quota and replay the accepted plan. | [Planner](Assets/Samples/RoundPlanner/Runtime/Core/RoundPlanner.cs) · [Guide](Assets/Samples/RoundPlanner/README.md) · [Checks](Tests/RoundPlanner/Program.cs) |
| **Match recovery** | [Play](https://sciuzza84.github.io/demos/unity/?sample=match-recovery) · Deliver revision 1, skip revision 2, deliver revision 3, then compare an old snapshot with a covering snapshot. | [Replica model](Assets/Samples/MatchRecovery/Runtime/Core/MatchReplica.cs) · [Guide](Assets/Samples/MatchRecovery/README.md) · [Checks](Tests/MatchRecovery/Program.cs) |
| **Launch handoff** | [Play](https://sciuzza84.github.io/demos/unity/?sample=launch-handoff) · Run the comparison and read the velocity and displacement from two Rigidbody traces. | [Unity driver](Assets/Samples/LaunchHandoff/Runtime/View/LaunchHandoffExplorer.cs) · [Guide](Assets/Samples/LaunchHandoff/README.md) · [Checks](Tests/LaunchHandoff/Program.cs) |
| **Daily challenge** | [Play](https://sciuzza84.github.io/demos/unity/?sample=daily-challenge) · Retry a submitted event, complete the day, then claim twice and advance to the next simulated day. | [Challenge ledger](Assets/Samples/DailyChallenge/Runtime/Core/DailyChallengeLedger.cs) · [Guide](Assets/Samples/DailyChallenge/README.md) · [Checks](Tests/DailyChallenge/Program.cs) |

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

The generated HTML references player files by content hash. When publishing an update, keep the preceding files available until cached copies of the previous HTML have expired.

## Run the checks

From the `samples` directory, use Python 3 and the **.NET 8 SDK**, or use a newer SDK with the .NET 8 runtime installed. These checks compile the same C# core used by Unity.

```sh
python check_samples.py
python check_samples.py Dungeon MatchRecovery
```

To run a single project directly, use the corresponding command below.

```sh
dotnet run --project Tests/Dungeon/Dungeon.Checks.csproj -c Release
dotnet run --project Tests/ArPlacement/ArPlacement.Checks.csproj -c Release
dotnet run --project Tests/ContentUpdates/ContentUpdates.Checks.csproj -c Release
dotnet run --project Tests/LevelEditor/LevelEditor.Checks.csproj -c Release
dotnet run --project Tests/Combat/Combat.Checks.csproj -c Release
dotnet run --project Tests/EvmLite/EvmLite.Checks.csproj -c Release
dotnet run --project Tests/AdaptiveLayout/AdaptiveLayout.Checks.csproj -c Release
dotnet run --project Tests/LayeredLayout/LayeredLayout.Checks.csproj -c Release
dotnet run --project Tests/RoundPlanner/RoundPlanner.Checks.csproj -c Release
dotnet run --project Tests/MatchRecovery/MatchRecovery.Checks.csproj -c Release
dotnet run --project Tests/LaunchHandoff/LaunchHandoff.Checks.csproj -c Release
dotnet run --project Tests/DailyChallenge/DailyChallenge.Checks.csproj -c Release
```

Four samples include Unity batch verification. [Level Editor](Assets/Samples/LevelEditor/README.md) preserves the draft asset GUID on save. [Layered layout](Assets/Samples/LayeredLayout/README.md) restores linked outputs on a simulated caught write failure. [Adaptive Layout](Assets/Samples/AdaptiveLayout/README.md) commits valid viewport changes to Unity components. [Launch Handoff](Assets/Samples/LaunchHandoff/README.md) runs the Rigidbody comparison.

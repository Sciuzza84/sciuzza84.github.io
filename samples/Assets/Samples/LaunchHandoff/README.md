# Launch Handoff

Compare two ways to release a guided Rigidbody into physics. This original sample draws on Cristiano Piromalli’s work on the handoff from guided movement to Rigidbody launches at Affinity Project. The two lanes use synthetic markers and a straight guide.

## Try it

Open [LaunchHandoff.unity](Scenes/LaunchHandoff.unity) and press Play. Select **Run comparison**. Both balls follow the same guide for 0.9 seconds, at about 4.00 m/s across and 1.22 m/s up. At the release marker, **Zero velocity** begins its fall from rest. **Preserve velocity** continues with the guide's velocity.

The trace keeps the actual sampled trajectory. Compare horizontal travel after about 0.6 seconds of physics. Expect about 0.00 m and 2.40 m respectively. Both lanes apply the same gravity. The narrow view stacks the traces vertically.

## Read and verify

- [Pure guide and release math](Runtime/Core/LaunchGuide.cs)
- [Unity Rigidbody driver and trace](Runtime/View/LaunchHandoffExplorer.cs)
- [Standalone checks](../../../Tests/LaunchHandoff/Program.cs)
- [Unity batch physics check](Editor/LaunchHandoffVerification.cs)

The guide computes velocity from the full displacement and duration. The Unity driver sets each body to the exact endpoint, makes it dynamic, then assigns its release velocity. It records Rigidbody position and velocity from completed physics steps and freezes the view at the end for inspection. The sample isolates linear velocity with freely falling bodies.

Run `dotnet run --project Tests/LaunchHandoff/LaunchHandoff.Checks.csproj -c Release` from the repository root. The checks cover the guide endpoints, step-size independence of release velocity, both release modes, and invalid inputs.

Run a separate Unity batch invocation from the repository root to verify the two Rigidbody trajectories with actual 0.02-second physics steps. The log reports the displacement after 0.60 seconds.

```sh
Unity -batchmode -nographics -quit -projectPath . -executeMethod GameplaySamples.LaunchHandoff.Editor.LaunchHandoffVerification.VerifySample -logFile -
```

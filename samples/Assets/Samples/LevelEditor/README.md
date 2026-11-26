# Level Editor

An original Unity authoring sample inspired by the level-design workflow of *Bounce That Bird!*.

1. Choose **Samples → Open Level Editor**, select **Gentle Ramp** or **Broken Bridge**, and click **Copy to Draft**. Both are original example layouts.
2. Select **Rock**, **Spring**, **Goal** or **Erase**, then click cells in **Edit Draft**. The read-only **Live Preview** shows the current layout. Edits apply to the draft copy.
3. Choose **Save Draft** to write the ScriptableObject. Make another change and choose **Reopen Draft**. **Keep Editing** preserves the change, while **Reopen** restores the saved arrangement.

The tool keeps the current board if it encounters an invalid draft. The underlying board rejects overlapping coordinates, invalid tile types and placements outside the 8 × 5 grid.

- [Editor window](Editor/LevelEditorWindow.cs)
- [Board rules](Runtime/Core/LevelBoard.cs)
- [Asset verification](Editor/LevelEditorVerification.cs)

Run `dotnet run --project Tests/LevelEditor/LevelEditor.Checks.csproj -c Release` from the repository root. The Editor batch check `GameplaySamples.LevelEditor.Editor.LevelEditorVerification.VerifySample` writes a temporary asset, reloads it from disk and verifies the existing draft format, template independence and an edit that keeps the asset GUID.

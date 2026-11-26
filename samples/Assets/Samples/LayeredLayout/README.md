# Layered Layout

Turn layered artwork positions into a puzzle prefab and level data. Cristiano built an Editor tool for *Love, Diana Hidden Objects* that carried the artist's named Photoshop layers into Unity. This independent Unity sample starts with synthetic JSON describing the canvas and its layers.

## Try the Web preview

Open the gallery at `?sample=layered-layout`. Select **Move beacon** to change the beacon from `(304, 122)` to `(344, 160)` while the other items stay in place. **Try invalid name** identifies the `beacon` layer that breaks the naming rule and keeps the moved layout visible. **Try invalid position** shows the bounds check. Select any data row to highlight the corresponding marker.

## Try the workflow in the Unity Editor

1. Select [Valid.json](Resources/LayeredLayout/Valid.json) in the Project window.
2. Choose **Samples → Layered Layout → Import Selected JSON**.
3. Inspect `Generated/LayeredLayout.prefab` and `Generated/LayeredLayoutData.asset` in the sample folder. The prefab contains three markers and references the data asset on its `ImportedLayerLayout` component. The data asset stores the canvas dimensions and the named pixel positions in layer order. Prefab positions use a centered local origin, 100 pixels per Unity unit and an inverted Y axis relative to the source.
4. Import [Moved.json](Resources/LayeredLayout/Moved.json) through the same command. The two assets keep their GUIDs and the beacon moves. Import it again to reuse the same outputs. Try [InvalidName.json](Resources/LayeredLayout/InvalidName.json) or [Invalid.json](Resources/LayeredLayout/Invalid.json) to see the previous outputs preserved.

The source contains one-based coordinates measured from the upper left. Each layer must have a unique `item_` name followed by lowercase letters or digits, a positive unique order, and coordinates within the canvas. The importer validates the full document before touching either asset.

The browser uses the same fixtures, sorting, and validation. The Unity Editor writes both assets.

## Read and verify

- [Validation and sorting](Runtime/Core/LayerMap.cs)
- [Prefab and data importer](Editor/LayeredLayoutImporter.cs)
- [Serialized level data](Runtime/View/PuzzleLayoutAsset.cs)
- [Standalone checks](../../../Tests/LayeredLayout/Program.cs)

Run `dotnet run --project Tests/LayeredLayout/LayeredLayout.Checks.csproj -c Release` from the Unity project directory (`samples/` in the portfolio repository). To verify both Unity outputs, run the Editor in batch mode with `-executeMethod GameplaySamples.LayeredLayout.Editor.LayeredLayoutImporter.VerifySample`. The Editor check verifies positions, links, file bytes, metadata and GUIDs across unchanged, changed and rejected inputs.

The batch check also performs both writes on test assets, then simulates an I/O error in the prefab writer. The importer restores the last saved prefab, level data and metadata. The check confirms that Unity can reuse and reload that restored pair.

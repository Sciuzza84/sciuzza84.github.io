using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GameplaySamples.LayeredLayout.Editor
{
    public static class LayeredLayoutImporter
    {
        private const string SampleFolder = "Assets/Samples/LayeredLayout";
        private const string FixtureFolder = SampleFolder + "/Resources/LayeredLayout";
        private const string Output = SampleFolder + "/Generated/LayeredLayout.prefab";
        private const float PixelsPerUnit = 100f;
        private static readonly Vector3 MarkerScale = new Vector3(0.20f, 0.20f, 1f);

        [MenuItem("Samples/Layered Layout/Import Selected JSON")]
        private static void ImportSelected()
        {
            if (!(Selection.activeObject is TextAsset source) ||
                !AssetDatabase.GetAssetPath(source).EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                Debug.LogError("Select a layout JSON asset in the Project window.");
                return;
            }

            if (!TryImport(source.text, Output, out GameObject prefab, out string result))
            {
                Debug.LogError(result, source);
                return;
            }
            Debug.Log(result, prefab);
            EditorGUIUtility.PingObject(prefab);
        }

        public static void VerifySample()
        {
            string temporaryName = "LayeredLayoutImportCheck-" + Guid.NewGuid().ToString("N");
            string temporaryPath = "Assets/" + temporaryName;
            string output = temporaryPath + "/LayeredLayout.prefab";
            if (string.IsNullOrEmpty(AssetDatabase.CreateFolder("Assets", temporaryName)))
                throw new InvalidOperationException("Unable to create the import test folder.");

            try
            {
                TextAsset valid = AssetDatabase.LoadAssetAtPath<TextAsset>(FixtureFolder + "/Valid.json");
                TextAsset moved = AssetDatabase.LoadAssetAtPath<TextAsset>(FixtureFolder + "/Moved.json");
                TextAsset invalidBounds = AssetDatabase.LoadAssetAtPath<TextAsset>(FixtureFolder + "/Invalid.json");
                TextAsset invalidName = AssetDatabase.LoadAssetAtPath<TextAsset>(FixtureFolder + "/InvalidName.json");
                Check(valid && moved && invalidBounds && invalidName, "All source fixtures must import as text assets.");
                Check(TryImport(valid.text, output, out GameObject prefab, out string result), result);
                Check(prefab.transform.childCount == 3, "Expected three generated children.");
                Check(prefab.transform.GetChild(0).name == "item_key" &&
                    prefab.transform.GetChild(1).name == "item_satchel" &&
                    prefab.transform.GetChild(2).name == "item_beacon", "Layer order changed in the prefab.");
                Check(prefab.transform.GetChild(0).localPosition == new Vector3(-1.08f, 0.54f, -0.01f),
                    "The first layer position does not match the source document.");

                string dataPath = GetDataPath(output);
                string prefabGuid = AssetDatabase.AssetPathToGUID(output);
                string dataGuid = AssetDatabase.AssetPathToGUID(dataPath);
                Check(!string.IsNullOrEmpty(prefabGuid) && !string.IsNullOrEmpty(dataGuid), "Both outputs must have a GUID.");
                CheckOutput(output, valid, new Vector3(0.94f, 0.18f, -0.03f));
                var initialPrefab = new AssetBackup(output);
                var initialData = new AssetBackup(dataPath);
                Check(TryImport(valid.text, output, out _, out result) && result.StartsWith("Layout unchanged"), result);
                Check(initialPrefab.IsUnchanged() && initialData.IsUnchanged(), "Identical input touched an output or meta file.");

                Check(TryImport(moved.text, output, out _, out result), result);
                CheckOutput(output, moved, new Vector3(1.34f, -0.20f, -0.03f));
                Check(prefabGuid == AssetDatabase.AssetPathToGUID(output) && dataGuid == AssetDatabase.AssetPathToGUID(dataPath),
                    "A changed layer must preserve both output GUIDs.");

                var updatedPrefab = new AssetBackup(output);
                var updatedData = new AssetBackup(dataPath);
                Check(!TryImport(invalidBounds.text, output, out _, out result) && result.Contains("falls outside the canvas"),
                    "Invalid layer bounds should be rejected.");
                Check(!TryImport(invalidName.text, output, out _, out result) && result.Contains("'beacon'"),
                    "The error should identify which layer name must change.");
                Check(updatedPrefab.IsUnchanged() && updatedData.IsUnchanged(),
                    "A rejected source modified the previous assets or their metadata.");
                CheckOutput(output, moved, new Vector3(1.34f, -0.20f, -0.03f));
                Debug.Log("Layered Layout PASS: prefab/data link, disk reload, moved layer, stable GUIDs, idempotence and rejection.");
            }
            finally
            {
                if (!AssetDatabase.DeleteAsset(temporaryPath))
                    Debug.LogWarning($"Could not remove temporary verification assets at {temporaryPath}.");
            }
        }

        private static bool TryImport(string json, string output, out GameObject prefab, out string result)
        {
            prefab = null;
            if (!LayerJson.TryRead(json, out LayerMap layout, out result))
                return false;

            string dataPath = GetDataPath(output);
            UnityEngine.Object prefabTarget = AssetDatabase.LoadMainAssetAtPath(output);
            UnityEngine.Object dataTarget = AssetDatabase.LoadMainAssetAtPath(dataPath);
            GameObject previous = prefabTarget as GameObject;
            var previousData = dataTarget as PuzzleLayoutAsset;
            if (File.Exists(output) && (!previous || !previous.TryGetComponent(out ImportedLayerLayout _)))
            {
                result = $"The prefab path is occupied by an asset created outside this tool: {output}.";
                return false;
            }
            if (File.Exists(dataPath) && !previousData)
            {
                result = $"The level data path is occupied by an asset created outside this tool: {dataPath}.";
                return false;
            }

            bool reuseData = previousData && previousData.Matches(layout);
            if (reuseData && previous && MatchesLayout(previous, previousData, layout))
            {
                prefab = previous;
                result = "Layout unchanged. Reused the existing prefab and level data.";
                return true;
            }

            return TrySave(layout, output, dataPath, previousData, reuseData, out prefab, out result);
        }

        private static bool TrySave(LayerMap layout, string output, string dataPath, PuzzleLayoutAsset data, bool reuseData,
            out GameObject prefab, out string result)
        {
            prefab = null;
            string directory = Path.GetDirectoryName(output).Replace(Path.DirectorySeparatorChar, '/');
            if (!AssetDatabase.IsValidFolder(directory) &&
                string.IsNullOrEmpty(AssetDatabase.CreateFolder(directory.Substring(0, directory.LastIndexOf('/')),
                    Path.GetFileName(directory))))
            {
                result = $"Unable to create the output folder at {directory}.";
                return false;
            }

            var root = new GameObject("LayeredLayout");
            AssetBackup prefabBackup = null;
            AssetBackup dataBackup = null;
            try
            {
                // Capture both destinations before writing the linked outputs.
                prefabBackup = new AssetBackup(output);
                dataBackup = new AssetBackup(dataPath);
                for (int index = 0; index < layout.Layers.Count; index++)
                {
                    ReadOnlyLayer layer = layout.Layers[index];
                    var marker = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    UnityEngine.Object.DestroyImmediate(marker.GetComponent<Collider>());
                    marker.name = layer.Name;
                    marker.transform.SetParent(root.transform, false);
                    marker.transform.localPosition = ToLocalPosition(layout, layer, index);
                    marker.transform.localScale = MarkerScale;
                }

                if (!reuseData)
                    data = SaveData(data, dataPath, layout);
                root.AddComponent<ImportedLayerLayout>().SetLayout(data);
                prefab = PrefabUtility.SaveAsPrefabAsset(root, output, out bool saved);
                if (!saved || !prefab)
                    throw new IOException($"Unable to save {output}.");

                result = $"Imported {layout.Layers.Count} layers. Prefab: {output} Level data: {dataPath}";
                return true;
            }
            catch (Exception failure) when (IsWriteFailure(failure))
            {
                prefab = null;
                string prefabRestoreError = prefabBackup?.TryRestore();
                string dataRestoreError = dataBackup?.TryRestore();
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                result = string.IsNullOrEmpty(prefabRestoreError) && string.IsNullOrEmpty(dataRestoreError)
                    ? $"Import failed. Previous outputs preserved. {failure.Message}"
                    : $"Import failed. Restore needed: {prefabRestoreError} {dataRestoreError} {failure.Message}";
                return false;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static PuzzleLayoutAsset SaveData(PuzzleLayoutAsset data, string output, LayerMap layout)
        {
            if (data)
            {
                data.SetLayout(layout);
                EditorUtility.SetDirty(data);
                AssetDatabase.SaveAssetIfDirty(data);
            }
            else
            {
                data = ScriptableObject.CreateInstance<PuzzleLayoutAsset>();
                try
                {
                    data.SetLayout(layout);
                    AssetDatabase.CreateAsset(data, output);
                }
                finally
                {
                    if (!EditorUtility.IsPersistent(data))
                        UnityEngine.Object.DestroyImmediate(data);
                }
            }

            if (!data || !File.Exists(output) || EditorUtility.IsDirty(data))
                throw new IOException($"Unable to save {output}.");
            return data;
        }

        private static bool MatchesLayout(GameObject prefab, PuzzleLayoutAsset data, LayerMap layout)
        {
            var info = prefab.GetComponent<ImportedLayerLayout>();
            if (!info || info.PuzzleData != data || info.Width != layout.Width || info.Height != layout.Height ||
                prefab.transform.childCount != layout.Layers.Count)
                return false;

            for (int index = 0; index < layout.Layers.Count; index++)
            {
                Transform child = prefab.transform.GetChild(index);
                ReadOnlyLayer layer = layout.Layers[index];
                if (child.name != layer.Name || child.localPosition != ToLocalPosition(layout, layer, index) ||
                    child.localScale != MarkerScale)
                    return false;
            }
            return true;
        }

        private static void CheckOutput(string output, TextAsset fixture, Vector3 expectedBeaconPosition)
        {
            const ImportAssetOptions options = ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate;
            string dataPath = GetDataPath(output);
            AssetDatabase.ImportAsset(dataPath, options);
            AssetDatabase.ImportAsset(output, options);

            Check(LayerJson.TryRead(fixture.text, out LayerMap layout, out string error), error);
            var data = AssetDatabase.LoadAssetAtPath<PuzzleLayoutAsset>(dataPath);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(output);
            Check(data && data.Matches(layout), "The saved asset must retain the ordered source pixel data.");
            Check(prefab && MatchesLayout(prefab, data, layout), "The prefab must retain its placements and the data reference.");
            Check(prefab.transform.GetChild(2).localPosition == expectedBeaconPosition, "The beacon did not keep its saved position.");
        }

        private static string GetDataPath(string prefabPath) => Path.ChangeExtension(prefabPath, null) + "Data.asset";

        private static bool IsWriteFailure(Exception failure) =>
            failure is IOException || failure is UnauthorizedAccessException || failure is UnityException;

        private static Vector3 ToLocalPosition(LayerMap layout, ReadOnlyLayer layer, int index)
        {
            // Artwork uses a top-left origin. World coordinates use Y increasing upwards.
            return new Vector3((layer.PixelX - layout.Width / 2f) / PixelsPerUnit,
                (layout.Height / 2f - layer.PixelY) / PixelsPerUnit, -(index + 1) * 0.01f);
        }

        private static void Check(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }

        private sealed class AssetBackup
        {
            private readonly string path;
            private readonly byte[] asset;
            private readonly byte[] metadata;

            public AssetBackup(string path)
            {
                this.path = path;
                asset = Read(path);
                metadata = Read(path + ".meta");
            }

            public bool IsUnchanged() => SameBytes(asset, Read(path)) && SameBytes(metadata, Read(path + ".meta"));

            public string TryRestore()
            {
                try
                {
                    UnityEngine.Object loaded = AssetDatabase.LoadMainAssetAtPath(path);
                    if (loaded && EditorUtility.IsDirty(loaded))
                        EditorUtility.ClearDirty(loaded);
                    RestoreFile(path, asset);
                    RestoreFile(path + ".meta", metadata);
                    return null;
                }
                catch (Exception failure) when (IsWriteFailure(failure))
                {
                    return $"{path}: {failure.Message}";
                }
            }

            private static byte[] Read(string file) => File.Exists(file) ? File.ReadAllBytes(file) : null;

            private static bool SameBytes(byte[] left, byte[] right) =>
                ReferenceEquals(left, right) || (left != null && right != null && left.SequenceEqual(right));

            private static void RestoreFile(string file, byte[] previous)
            {
                if (SameBytes(previous, Read(file)))
                    return;
                if (previous == null)
                    File.Delete(file);
                else
                    File.WriteAllBytes(file, previous);
            }
        }
    }
}

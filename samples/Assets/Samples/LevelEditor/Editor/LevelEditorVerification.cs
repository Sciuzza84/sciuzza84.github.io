using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GameplaySamples.LevelEditor.Editor
{
    public static class LevelEditorVerification
    {
        public static void VerifySample()
        {
            string name = "LevelEditorCheck-" + Guid.NewGuid().ToString("N");
            string folder = "Assets/" + name;
            string path = folder + "/LevelDraft.asset";
            Check(!string.IsNullOrEmpty(AssetDatabase.CreateFolder("Assets", name)), "Temporary asset folder.");

            try
            {
                var board = new LevelBoard();
                Check(board.TrySet(0, 0, TileKind.Rock) && board.TrySet(3, 2, TileKind.Spring) &&
                    board.TrySet(7, 4, TileKind.Goal), "Valid placements.");
                Check(LevelDraftAssets.TrySave(path, board, out string error), error);
                string originalGuid = AssetDatabase.AssetPathToGUID(path);
                Check(!string.IsNullOrEmpty(originalGuid) && File.Exists(path), "Serialized draft.");
                Reload(path);
                Check(LevelDraftAssets.TryOpen(path, out LevelBoard opened, out error), error);
                Check(opened.Snapshot().SequenceEqual(board.Snapshot()), "A draft without a template reopens unchanged.");

                var template = LevelBoard.CreateDraft(LevelTemplate.GentleRamp).Snapshot();
                var copied = LevelBoard.CreateDraft(LevelTemplate.GentleRamp);
                Check(copied.At(0, 4) == TileKind.Rock && copied.TrySet(0, 4, TileKind.Empty), "Erase a rock from the copied draft.");
                Check(copied.TrySet(5, 1, TileKind.Spring), "Add a spring to the copied draft.");
                Check(LevelDraftAssets.TrySave(path, copied, out error), error);
                Reload(path);
                Check(LevelDraftAssets.TryOpen(path, out opened, out error), error);
                Check(opened.Snapshot().SequenceEqual(copied.Snapshot()), "The edited copy reopens unchanged.");

                Check(opened.TrySet(5, 1, TileKind.Empty), "Erase the new spring.");
                Check(LevelDraftAssets.TrySave(path, opened, out error), error);
                Reload(path);
                Check(LevelDraftAssets.TryOpen(path, out LevelBoard edited, out error), error);
                Check(edited.At(5, 1) == TileKind.Empty && edited.Snapshot().Count == template.Count - 1 &&
                    AssetDatabase.AssetPathToGUID(path) == originalGuid, "Edit must update the same asset.");
                Check(LevelBoard.CreateDraft(LevelTemplate.GentleRamp).Snapshot().SequenceEqual(template), "Saving and reopening a draft leave the template unchanged.");
                Debug.Log("Level Editor PASS: existing draft format, independent template copy, editable preview, disk reload and stable asset GUID.");
            }
            finally
            {
                if (!AssetDatabase.DeleteAsset(folder))
                    Debug.LogWarning($"Could not remove temporary verification assets at {folder}.");
            }
        }

        private static void Reload(string path)
        {
            Resources.UnloadAsset(AssetDatabase.LoadAssetAtPath<LevelDraft>(path));
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        }

        private static void Check(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }
    }
}

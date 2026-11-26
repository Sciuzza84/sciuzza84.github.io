using System.IO;
using UnityEditor;
using UnityEngine;

namespace GameplaySamples.LevelEditor.Editor
{
    internal static class LevelDraftAssets
    {
        public static bool TrySave(string path, LevelBoard board, out string error)
        {
            var existing = AssetDatabase.LoadMainAssetAtPath(path);
            if (existing && !(existing is LevelDraft))
            {
                error = $"Another asset occupies {path}.";
                return false;
            }
            if (existing is LevelDraft saved)
            {
                saved.SetBoard(board);
                EditorUtility.SetDirty(saved);
                AssetDatabase.SaveAssetIfDirty(saved);
            }
            else
            {
                string directory = Path.GetDirectoryName(path).Replace(Path.DirectorySeparatorChar, '/');
                if (!AssetDatabase.IsValidFolder(directory) &&
                    string.IsNullOrEmpty(AssetDatabase.CreateFolder(directory.Substring(0, directory.LastIndexOf('/')),
                        Path.GetFileName(directory))))
                {
                    error = $"Could not create {directory}.";
                    return false;
                }
                var created = ScriptableObject.CreateInstance<LevelDraft>();
                created.SetBoard(board);
                AssetDatabase.CreateAsset(created, path);
                AssetDatabase.SaveAssetIfDirty(created);
            }

            error = null;
            return true;
        }

        public static bool TryOpen(string path, out LevelBoard board, out string error)
        {
            var draft = AssetDatabase.LoadAssetAtPath<LevelDraft>(path);
            if (draft)
                return draft.TryRead(out board, out error);

            board = null;
            error = "Save a draft before reopening it.";
            return false;
        }
    }
}

using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace GameplaySamples.Editor
{
    public static class WebPlayerBuild
    {
        public const string DungeonScene = "Assets/Samples/Dungeon/Scenes/Dungeon.unity";
        private const string GalleryScene = "Assets/Samples/Gallery/Samples.unity";

        [MenuItem("Samples/Build Dungeon for Web")]
        public static void BuildDungeon()
        {
            Build(new[] { DungeonScene }, "Builds/Web/Dungeon");
        }

        [MenuItem("Samples/Build Gallery for Web")]
        public static void BuildGallery()
        {
            string[] scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled)
                .Select(scene => scene.path).ToArray();
            if (scenes.Length == 0 || scenes[0] != GalleryScene)
                throw new BuildFailedException("Set Samples.unity as the first enabled build scene.");

            Build(scenes, "Builds/Web/Gallery");
        }

        private static void Build(string[] scenes, string output)
        {
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                target = BuildTarget.WebGL,
                locationPathName = output,
                options = BuildOptions.StrictMode
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new BuildFailedException($"Web build failed: {report.summary.result}");

            Debug.Log($"Web player built in {output} ({report.summary.totalSize:N0} bytes).");
        }
    }
}
